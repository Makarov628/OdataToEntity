﻿using System;
using System.Collections.Generic;
using System.Globalization;

namespace OdataToEntity.EfCore.DynamicDataContext.InformationSchema
{
    public readonly struct Navigation
    {
        public Navigation(String constraintSchema, String dependentConstraintName, String principalConstraintName, String navigationName, bool isCollection)
        {
            ConstraintSchema = constraintSchema;
            DependentConstraintName = dependentConstraintName;
            PrincipalConstraintName = principalConstraintName;
            NavigationName = navigationName;
            IsCollection = isCollection;
        }

        public String ConstraintSchema { get; }
        public String DependentConstraintName { get; }
        public String NavigationName { get; }
        public bool IsCollection { get; }
        public String PrincipalConstraintName { get; }

        public static Dictionary<TableFullName, List<Navigation>> GetNavigations(
            IReadOnlyList<ReferentialConstraint> referentialConstraints,
            IReadOnlyDictionary<(String constraintSchema, String constraintName), IReadOnlyList<KeyColumnUsage>> keyColumns,
            IReadOnlyDictionary<TableFullName, (String tableEdmName, bool isQueryType)> tableFullNameEdmNames,
            IReadOnlyDictionary<TableFullName, IReadOnlyList<NavigationMapping>> navigationMappings,
            IReadOnlyDictionary<TableFullName, List<Column>> tableColumns)
        {
            var tableNavigations = new Dictionary<TableFullName, List<Navigation>>();
            var navigationCounter = new Dictionary<(String, String, String), List<IReadOnlyList<KeyColumnUsage>>>();
            foreach (ReferentialConstraint fkey in referentialConstraints)
            {
                IReadOnlyList<KeyColumnUsage> dependentKeyColumns = keyColumns[(fkey.ConstraintSchema, fkey.ConstraintName)];

                KeyColumnUsage dependentKeyColumn = dependentKeyColumns[0];
                var dependentFullName = new TableFullName(dependentKeyColumn.TableSchema, dependentKeyColumn.TableName);
                if (!tableFullNameEdmNames.TryGetValue(dependentFullName, out (String principalEdmName, bool _) p))
                    continue;

                KeyColumnUsage principalKeyColumn = keyColumns[(fkey.UniqueConstraintSchema, fkey.UniqueConstraintName)][0];
                var principalFullName = new TableFullName(principalKeyColumn.TableSchema, principalKeyColumn.TableName);
                if (!tableFullNameEdmNames.TryGetValue(principalFullName, out (String dependentEdmName, bool _) d))
                    continue;

                bool selfReferences = false;
                String? dependentNavigationName = GetNavigationMappingName(navigationMappings, fkey, dependentKeyColumn);
                if (dependentNavigationName == null)
                {
                    selfReferences = dependentKeyColumn.TableSchema == principalKeyColumn.TableSchema && dependentKeyColumn.TableName == principalKeyColumn.TableName;
                    if (selfReferences)
                        dependentNavigationName = "Parent";
                    else
                        dependentNavigationName = Humanizer.InflectorExtensions.Singularize(d.dependentEdmName);

                    (String, String, String) dependentKey = (fkey.ConstraintSchema, dependentKeyColumn.TableName, principalKeyColumn.TableName);
                    if (navigationCounter.TryGetValue(dependentKey, out List<IReadOnlyList<KeyColumnUsage>>? columnsList))
                    {
                        if (FKeyExist(columnsList, dependentKeyColumns))
                            continue;

                        columnsList.Add(dependentKeyColumns);
                    }
                    else
                    {
                        columnsList = new List<IReadOnlyList<KeyColumnUsage>>() { dependentKeyColumns };
                        navigationCounter[dependentKey] = columnsList;
                    }

                    List<Column> dependentColumns = tableColumns[dependentFullName];
                    dependentNavigationName = GetUniqueName(dependentColumns, dependentNavigationName, columnsList.Count);
                }

                String? principalNavigationName = GetNavigationMappingName(navigationMappings, fkey, principalKeyColumn);
                if (principalNavigationName == null)
                {
                    if (dependentKeyColumn.TableSchema == principalKeyColumn.TableSchema && dependentKeyColumn.TableName == principalKeyColumn.TableName)
                        principalNavigationName = "Children";
                    else
                        principalNavigationName = Humanizer.InflectorExtensions.Pluralize(p.principalEdmName);

                    (String, String, String) principalKey = (fkey.ConstraintSchema, principalKeyColumn.TableName, dependentKeyColumn.TableName);
                    if (navigationCounter.TryGetValue(principalKey, out List<IReadOnlyList<KeyColumnUsage>>? columnsList))
                    {
                        if (!selfReferences)
                        {
                            if (FKeyExist(columnsList, dependentKeyColumns))
                                continue;

                            columnsList.Add(dependentKeyColumns);
                        }
                    }
                    else
                    {
                        columnsList = new List<IReadOnlyList<KeyColumnUsage>>() { dependentKeyColumns };
                        navigationCounter[principalKey] = columnsList;
                    }

                    List<Column> principalColumns = tableColumns[principalFullName];
                    principalNavigationName = GetUniqueName(principalColumns, principalNavigationName, columnsList.Count);
                }

                AddNavigation(tableNavigations, fkey, dependentKeyColumn, dependentNavigationName, false);
                AddNavigation(tableNavigations, fkey, principalKeyColumn, principalNavigationName, true);
            }

            return tableNavigations;

            static void AddNavigation(Dictionary<TableFullName, List<Navigation>> tableNavigations,
                ReferentialConstraint fkey, KeyColumnUsage keyColumn, String navigationName, bool isCollection)
            {
                if (!String.IsNullOrEmpty(navigationName))
                {
                    var tableFullName = new TableFullName(keyColumn.TableSchema, keyColumn.TableName);
                    if (!tableNavigations.TryGetValue(tableFullName, out List<Navigation>? principalNavigations))
                    {
                        principalNavigations = new List<Navigation>();
                        tableNavigations.Add(tableFullName, principalNavigations);
                    }
                    principalNavigations.Add(new Navigation(fkey.ConstraintSchema, fkey.ConstraintName, fkey.UniqueConstraintName, navigationName, isCollection));
                }
            }
            static bool FKeyExist(List<IReadOnlyList<KeyColumnUsage>> keyColumnsList, IReadOnlyList<KeyColumnUsage> keyColumns)
            {
                for (int i = 0; i < keyColumnsList.Count; i++)
                    if (keyColumnsList[i].Count == keyColumns.Count)
                    {
                        int j = 0;
                        for (; j < keyColumns.Count; j++)
                            if (keyColumnsList[i][j].ColumnName != keyColumns[j].ColumnName)
                                break;

                        if (j == keyColumns.Count)
                            return true;
                    }

                return false;
            }
            static int GetCountName(IReadOnlyList<Column> columns, String navigationName)
            {
                int counter = 0;
                for (int i = 0; i < columns.Count; i++)
                    if (String.Compare(navigationName, columns[i].ColumnName, StringComparison.OrdinalIgnoreCase) == 0)
                        counter++;
                return counter;
            }
            static String? GetNavigationMappingName(IReadOnlyDictionary<TableFullName, IReadOnlyList<NavigationMapping>> navigationMappings,
                ReferentialConstraint fkey, KeyColumnUsage keyColumn)
            {
                var tableFullName = new TableFullName(keyColumn.TableSchema, keyColumn.TableName);
                if (navigationMappings.TryGetValue(tableFullName, out IReadOnlyList<NavigationMapping>? tableNavigationMappings))
                    for (int i = 0; i < tableNavigationMappings.Count; i++)
                    {
                        NavigationMapping navigationMapping = tableNavigationMappings[i];
                        if (String.CompareOrdinal(navigationMapping.ConstraintName, fkey.ConstraintName) == 0)
                            return navigationMapping.NavigationName;
                    }

                return null;
            }
            static String GetUniqueName(IReadOnlyList<Column> columns, String navigationName, int counter)
            {
                int counter2;
                String navigationName2 = navigationName;
                do
                {
                    counter2 = GetCountName(columns, navigationName2);
                    counter += counter2;
                    navigationName2 = counter > 1 ? navigationName + counter.ToString(CultureInfo.InvariantCulture) : navigationName;
                }
                while (counter2 > 0 && GetCountName(columns, navigationName2) > 0);
                return navigationName2;
            }
        }
    }
}
