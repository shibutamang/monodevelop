﻿//
// PackageDependencyNode.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement.NodeBuilders
{
	class PackageDependencyNode
	{
		PackageDependenciesNode dependenciesNode;
		PackageDependency dependency;
		string name;
		string version;

		PackageDependencyNode (
			PackageDependenciesNode dependenciesNode,
			PackageDependency dependency,
			bool topLevel)
		{
			this.dependenciesNode = dependenciesNode;
			this.dependency = dependency;
			IsTopLevel = topLevel;

			name = dependency.Name;
			version = dependency.Version;

			if (IsTopLevel)
				IsReadOnly = !PackageReferenceExistsInProject ();
		}

		PackageDependencyNode (PackageDependenciesNode dependenciesNode, ProjectPackageReference packageReference)
		{
			this.dependenciesNode = dependenciesNode;
			IsTopLevel = true;

			name = packageReference.Include;
			version = packageReference.Metadata.GetValue ("Version", string.Empty);
		}

		public static PackageDependencyNode Create (
			PackageDependenciesNode dependenciesNode,
			string dependencyName,
			bool topLevel)
		{
			PackageDependency dependency = dependenciesNode.GetDependency (dependencyName);
			if (dependency != null)
				return new PackageDependencyNode (dependenciesNode, dependency, topLevel);

			return null;
		}

		public static PackageDependencyNode Create (
			PackageDependenciesNode dependenciesNode,
			ProjectPackageReference packageReference)
		{
			return new PackageDependencyNode (dependenciesNode, packageReference);
		}

		public string Name {
			get { return name; }
		}

		public string GetLabel ()
		{
			return GLib.Markup.EscapeText (Name);
		}

		public string GetSecondaryLabel ()
		{
			return string.Format ("({0})", version);
		}

		public IconId GetIconId ()
		{
			return new IconId ("md-package-dependency");
		}

		public DotNetProject Project {
			get { return dependenciesNode.Project; }
		}

		public bool IsTopLevel { get; private set; }
		public bool IsReadOnly { get; private set; }

		public bool CanBeRemoved {
			get { return IsTopLevel && !IsReadOnly; }
		}

		public bool IsReleaseVersion ()
		{
			NuGetVersion nugetVersion = null;
			if (NuGetVersion.TryParse (version, out nugetVersion)) {
				return !nugetVersion.IsPrerelease;
			}

			LoggingService.LogError ("Unable to parse NuGet package version '{0}'. Assuming release version.", version);
			return true;
		}

		public bool HasDependencies ()
		{
			if (dependency != null)
				return dependency.Dependencies.Any ();

			return false;
		}

		public IEnumerable<PackageDependencyNode> GetDependencyNodes ()
		{
			if (dependency != null)
				return GetDependencyNodes (dependenciesNode, dependency);

			return new PackageDependencyNode[0];
		}

		public static IEnumerable<PackageDependencyNode> GetDependencyNodes (
			PackageDependenciesNode dependenciesNode,
			PackageDependency dependency,
			bool topLevel = false)
		{
			return dependency.Dependencies
				.Select (item => PackageDependencyNode.Create (dependenciesNode, item, topLevel))
				.Where (item => item != null);
		}

		bool PackageReferenceExistsInProject ()
		{
			return dependenciesNode.Project.Items.OfType<ProjectPackageReference> ().Any (IsMatch);
		}

		bool IsMatch (ProjectPackageReference packageReference)
		{
			return StringComparer.OrdinalIgnoreCase.Equals (packageReference.Include, name);
		}
	}
}