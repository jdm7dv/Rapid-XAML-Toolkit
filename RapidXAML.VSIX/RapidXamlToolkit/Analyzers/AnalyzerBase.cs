﻿// <copyright file="AnalyzerBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using IServiceProvider = System.IServiceProvider;

namespace RapidXamlToolkit
{
    public class AnalyzerBase
    {
        public const string NamePlaceholder = "{NAME}";

        public const string NumericPlaceholder = "{X}";

        public const string RepeatedNumericPlaceholder = "{XX}";

        public const string TypePlaceholder = "{TYPE}";

        public const string SubPropertiesPlaceholder = "{SUBPROPERTIES}";

        // Can't support this unless able to get relevant information for all types (from all locations: file, project, solution, assembly)
        ////public const string SubPropertiesReadOnlyPlaceholder = "{SUBPROPERTIES-READONLY}";

        public const string NoPropertiesXaml = "<!-- No accessible properties when copying as XAML -->";

        public const string GridWithRowDefsIndicator = "GRID-PLUS-ROWDEFS";

        public const string GridWithRowDefs2ColsIndicator = "GRID-PLUS-ROWDEFS-2COLS";

        public static IServiceProvider ServiceProvider { get; set; }

        public string FileExtension { get; } = string.Empty;

        public static Settings GetSettings()
        {
            var configuredSettings = new ConfiguredSettings(ServiceProvider);

            return configuredSettings.ActualSettings;
        }

        public static string GetPropertyOutputForActiveProfile(string type, string name, bool isReadOnly, Func<(List<string> strings, int count)> getSubPropertyOutput = null)
        {
            return GetPropertyOutputForActiveProfile(type, name, isReadOnly, 1, getSubPropertyOutput).output;
        }

        public static (string output, int counter) GetPropertyOutputForActiveProfile(string type, string name, bool isReadOnly, int numericSubstitute, Func<(List<string> strings, int count)> getSubPropertyOutput = null)
        {
            var settings = GetSettings();
            var profile = settings.GetActiveProfile();
            return GetPropertyOutput(profile, type, name, isReadOnly, numericSubstitute, getSubPropertyOutput);
        }

        public static string GetClassGroupingForActiveProfile()
        {
            var settings = GetSettings();
            var profile = settings.GetActiveProfile();
            return profile.ClassGrouping.Trim();
        }

        public static string GetPropertyOutput(Profile profile, string type, string name, bool isReadOnly, Func<(List<string> strings, int count)> getSubProperties = null)
        {
            return GetPropertyOutput(profile, type, name, isReadOnly, 1, getSubProperties).output;
        }

        public static (string output, int counter) GetPropertyOutput(Profile profile, string type, string name, bool isReadOnly, int numericSubstitute, Func<(List<string> strings, int count)> getSubPropertyOutput = null)
        {
            var mappingOfInterest = GetMappingOfInterest(profile, type, name, isReadOnly);

            string rawOutput = null;

            if (mappingOfInterest != null)
            {
                rawOutput = mappingOfInterest.Output;
            }
            else
            {
                if (type.IsGenericTypeName())
                {
                    var wildcardGenericType = type.Substring(0, type.ToCSharpFormat().IndexOf("<", StringComparison.Ordinal)) + "<T>";

                    mappingOfInterest = GetMappingOfInterest(profile, wildcardGenericType, name, isReadOnly);

                    if (mappingOfInterest != null)
                    {
                        rawOutput = mappingOfInterest.Output;
                    }
                }

                if (rawOutput == null)
                {
                    rawOutput = profile?.DefaultOutput;
                }
            }

            if (rawOutput == null)
            {
                return (null, numericSubstitute);
            }

            var result = rawOutput.Replace(NamePlaceholder, name);

            if (type.IsGenericTypeName())
            {
                var typeArgument = type.Substring(type.IndexOf("<", StringComparison.Ordinal) + 1, type.Length - type.IndexOf("<", StringComparison.Ordinal) - 2);
                result = result.Replace(TypePlaceholder, typeArgument);
            }
            else
            {
                result = result.Replace(TypePlaceholder, type);
            }

            if (rawOutput.Contains(SubPropertiesPlaceholder))
            {
                bool subPropertyInsideGridPlusRowDefs =
                    (rawOutput.IndexOf(SubPropertiesPlaceholder, StringComparison.OrdinalIgnoreCase)
                    > rawOutput.IndexOf(GridWithRowDefsIndicator, StringComparison.OrdinalIgnoreCase)
                 && rawOutput.IndexOf("/" + GridWithRowDefsIndicator, StringComparison.OrdinalIgnoreCase)
                    > rawOutput.IndexOf(SubPropertiesPlaceholder, StringComparison.OrdinalIgnoreCase))
                    ||
                    (rawOutput.IndexOf(SubPropertiesPlaceholder, StringComparison.OrdinalIgnoreCase)
                    > rawOutput.IndexOf(GridWithRowDefs2ColsIndicator, StringComparison.OrdinalIgnoreCase)
                    && rawOutput.IndexOf("/" + GridWithRowDefs2ColsIndicator, StringComparison.OrdinalIgnoreCase)
                    > rawOutput.IndexOf(SubPropertiesPlaceholder, StringComparison.OrdinalIgnoreCase));

                var subProps = getSubPropertyOutput?.Invoke();

                var replacement = new StringBuilder();
                replacement.AppendLine();

                if (subProps != null)
                {
                    foreach (var subProp in subProps.Value.strings)
                    {
                        // TODO: remove this? or at least make it configurable? ISSUE #24
                        replacement.AppendLine(subProp.Replace("ViewModel.", string.Empty));
                    }
                }

                result = result.Replace(SubPropertiesPlaceholder, replacement.ToString());

                if (subPropertyInsideGridPlusRowDefs)
                {
                    var opener = new StringBuilder();

                    opener.AppendLine("<Grid>");

                    if (rawOutput.Contains(GridWithRowDefs2ColsIndicator))
                    {
                        opener.AppendLine("<Grid.ColumnDefinitions>");
                        opener.AppendLine("<ColumnDefinition Width=\"Auto\" />");
                        opener.AppendLine("<ColumnDefinition Width=\"*\" />");
                        opener.AppendLine("</Grid.ColumnDefinitions>");
                    }

                    if (subProps.HasValue)
                    {
                        opener.AppendLine("<Grid.RowDefinitions>");

                        for (int i = 1; i <= subProps.Value.count; i++)
                        {
                            opener.AppendLine(i < subProps.Value.count
                                ? "<RowDefinition Height=\"Auto\" />"
                                : "<RowDefinition Height=\"*\" />");
                        }

                        opener.Append("</Grid.RowDefinitions>");
                    }

                    var placeHolderPos = result.IndexOf(GridWithRowDefs2ColsIndicator, StringComparison.InvariantCultureIgnoreCase);
                    if (placeHolderPos > -1)
                    {
                        var placeHolderEndPos = result.IndexOf(">", placeHolderPos, StringComparison.InvariantCultureIgnoreCase);

                        result = result.Substring(0, placeHolderPos - 1) + opener + result.Substring(placeHolderEndPos + 1);

                        placeHolderPos = result.IndexOf("</" + GridWithRowDefs2ColsIndicator + ">", StringComparison.InvariantCultureIgnoreCase);

                        result = result.Substring(0, placeHolderPos) + "</Grid>" + result.Substring(placeHolderPos + GridWithRowDefs2ColsIndicator.Length + 3);
                    }

                    placeHolderPos = result.IndexOf(GridWithRowDefsIndicator, StringComparison.InvariantCultureIgnoreCase);

                    if (placeHolderPos > -1)
                    {
                        var placeHolderEndPos = result.IndexOf(">", placeHolderPos, StringComparison.InvariantCultureIgnoreCase);

                        result = result.Substring(0, placeHolderPos - 1) + opener + result.Substring(placeHolderEndPos + 1);

                        placeHolderPos = result.IndexOf("</" + GridWithRowDefsIndicator + ">", StringComparison.InvariantCultureIgnoreCase);

                        result = result.Substring(0, placeHolderPos) + "</Grid>" + result.Substring(placeHolderPos + GridWithRowDefsIndicator.Length + 3);
                    }
                }
            }

            while (result.Contains(NumericPlaceholder))
            {
                var subPosition = result.IndexOf(NumericPlaceholder, StringComparison.OrdinalIgnoreCase);

                result = result.Remove(subPosition, NumericPlaceholder.Length);
                result = result.Insert(subPosition, numericSubstitute.ToString());

                numericSubstitute += 1;
            }

            while (result.Contains(RepeatedNumericPlaceholder))
            {
                var subPosition = result.IndexOf(RepeatedNumericPlaceholder, StringComparison.OrdinalIgnoreCase);

                result = result.Remove(subPosition, RepeatedNumericPlaceholder.Length);
                result = result.Insert(subPosition, (numericSubstitute - 1).ToString()); // Remove 1 as was incremented after last used
            }

            return (result, numericSubstitute);
        }

        public static Mapping GetMappingOfInterest(Profile profile, string type, string name, bool isReadOnly)
        {
            if (profile == null)
            {
                return null;
            }

            var typeMappings = profile.Mappings.Where(m => type.ToCSharpFormat().MatchesAnyOf(m.Type));

            if (!isReadOnly)
            {
                typeMappings = typeMappings.Where(m => m.IfReadOnly == false);
            }

            Mapping mappingOfInterest = null;

            // Readonly types match readonly mappings first
            if (isReadOnly)
            {
                mappingOfInterest = typeMappings.FirstOrDefault(m => name.ToLowerInvariant().ContainsAnyOf(m.NameContains.ToLowerInvariant()) && m.IfReadOnly == true)
                                 ?? typeMappings.FirstOrDefault(m => string.IsNullOrWhiteSpace(m.NameContains) && m.IfReadOnly == true);
            }

            // writeable types don't match readonly mappings
            // readonly types match writeable mappings if no readdonly mappings
            if (mappingOfInterest == null)
            {
                mappingOfInterest = typeMappings.FirstOrDefault(m => name.ToLowerInvariant().ContainsAnyOf(m.NameContains.ToLowerInvariant()) && m.IfReadOnly == false)
                                 ?? typeMappings.FirstOrDefault(m => string.IsNullOrWhiteSpace(m.NameContains) && m.IfReadOnly == false);
            }

            return mappingOfInterest;
        }

        public static string GetSelectionPropertiesName(List<string> names)
        {
            if (names == null || !names.Any())
            {
                return string.Empty;
            }

            var result = names.First();

            if (names.Count == 2)
            {
                result += $" and {names[1]}";
            }
            else if (names.Count > 2)
            {
                var others = names.Count == 3 ? "other property" : "other properties";

                result += $", {names[1]} and {names.Count - 2} {others}";
            }

            return result;
        }

        public static string FormattedClassGroupingOpener(string classGrouping)
        {
            switch (classGrouping.ToUpperInvariant())
            {
                case GridWithRowDefsIndicator:
                case GridWithRowDefs2ColsIndicator:
                    return "Grid";
                default:
                    return classGrouping;
            }
        }

        public static string FormattedClassGroupingCloser(string classGrouping)
        {
            if (classGrouping.Contains(" "))
            {
                return classGrouping.Substring(0, classGrouping.IndexOf(" ", StringComparison.Ordinal));
            }

            switch (classGrouping.ToUpperInvariant())
            {
                case GridWithRowDefsIndicator:
                case GridWithRowDefs2ColsIndicator:
                    return "Grid";
                default:
                    return classGrouping;
            }
        }
    }
}