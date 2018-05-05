﻿// <copyright file="TestProfile.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;

namespace RapidXamlToolkit.Tests
{
    public static class TestProfile
    {
        public static Profile CreateEmpty()
        {
            return new Profile
            {
                Name = string.Empty,
                ClassGrouping = string.Empty,
                FallbackOutput = string.Empty,
                SubPropertyOutput = string.Empty,
                EnumMemberOutput = string.Empty,
                Mappings = new ObservableCollection<Mapping>(),
                ViewGeneration = new ViewGenerationSettings
                {
                    AllInSameProject = false,
                    CodePlaceholder = string.Empty,
                    ViewModelDirectoryName = string.Empty,
                    ViewModelFileSuffix = string.Empty,
                    ViewModelProjectSuffix = string.Empty,
                    XamlFileDirectoryName = string.Empty,
                    XamlFileSuffix = string.Empty,
                    XamlPlaceholder = string.Empty,
                    XamlProjectSuffix = string.Empty,
                },
                Datacontext = new DatacontextSettings
                {
                    CodeBehindConstructorContent = string.Empty,
                    CodeBehindPageContent = string.Empty,
                    DefaultCodeBehindConstructor = string.Empty,
                    XamlPageAttribute = string.Empty,
                },
            };
        }
    }
}
