// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// Suppress nullable warnings for WPF designer-generated code
[assembly: SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "WPF designer files should not be modified", Scope = "namespaceanddescendants", Target = "~N:YouTubeDownloader")]

// Suppress nullable warnings for event handlers that are set via XAML
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "WPF event handlers are called by the framework", Scope = "namespaceanddescendants", Target = "~N:YouTubeDownloader")]
