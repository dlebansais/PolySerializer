namespace PolySerializer
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Perform partial search/replace in assembly qualified type names.
    /// </summary>
    public struct NamespaceDescriptor
    {
        #region Constants
        /// <summary>
        /// The text to search to always replace.
        /// </summary>
        public static readonly string MatchAny = "*";

        /// <summary>
        /// A descriptor that will match all names.
        /// </summary>
        public static readonly NamespaceDescriptor MatchAll = new NamespaceDescriptor(MatchAny, MatchAny, MatchAny, MatchAny, MatchAny);

        private const string AssemblyPattern = " ";
        private const string VersionPattern = " Version=";
        private const string CulturePattern = " Culture=";
        private const string PublicKeyTokenPattern = " PublicKeyToken=";
        #endregion

        #region Init
        /// <summary>
        /// Initializes a new instance of the <see cref="NamespaceDescriptor"/> struct.
        /// </summary>
        /// <param name="path">The namespace path.</param>
        public NamespaceDescriptor(string path)
        {
            Path = path;
            Assembly = null;
            Version = null;
            Culture = null;
            PublicKeyToken = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NamespaceDescriptor"/> struct.
        /// </summary>
        /// <param name="path">The namespace path.</param>
        /// <param name="assembly">The assembly name.</param>
        public NamespaceDescriptor(string path, string assembly)
        {
            Path = path;
            Assembly = assembly;
            Version = null;
            Culture = null;
            PublicKeyToken = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NamespaceDescriptor"/> struct.
        /// </summary>
        /// <param name="path">The namespace path.</param>
        /// <param name="assembly">The assembly name.</param>
        /// <param name="version">The assembly version.</param>
        public NamespaceDescriptor(string path, string assembly, string version)
        {
            Path = path;
            Assembly = assembly;
            Version = version;
            Culture = null;
            PublicKeyToken = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NamespaceDescriptor"/> struct.
        /// </summary>
        /// <param name="path">The namespace path.</param>
        /// <param name="assembly">The assembly name.</param>
        /// <param name="version">The assembly version.</param>
        /// <param name="culture">The culture name.</param>
        /// <param name="publicKeyToken">The public key token.</param>
        public NamespaceDescriptor(string path, string assembly, string version, string culture, string publicKeyToken)
        {
            Path = path;
            Assembly = assembly;
            Version = version;
            Culture = culture;
            PublicKeyToken = publicKeyToken;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the namespace path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the assembly name.
        /// </summary>
        public string Assembly { get; set; }

        /// <summary>
        /// Gets or sets the assembly version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the culture name.
        /// </summary>
        public string Culture { get; set; }

        /// <summary>
        /// Gets or sets the public key token.
        /// </summary>
        public string PublicKeyToken { get; set; }
        #endregion

        #region Client Interface
        /// <summary>
        /// Performs a search replace on <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The assembly qualified type name to search in.</param>
        /// <param name="descriptorSearch">The descriptor with search parts.</param>
        /// <param name="descriptorReplace">The descriptor with replace prts.</param>
        /// <param name="nameOverride">The value of <paramref name="name"/> after replacements.</param>
        /// <returns>True if at least one searched part in <paramref name="name"/> was replaced.</returns>
        public static bool Match(string name, NamespaceDescriptor descriptorSearch, NamespaceDescriptor descriptorReplace, out string nameOverride)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            string[] FullNamePath = name.Split(',');

            bool IsValidName = FullNamePath.Length == 5 &&
                               FullNamePath[1].StartsWith(AssemblyPattern, StringComparison.InvariantCulture) &&
                               FullNamePath[2].StartsWith(VersionPattern, StringComparison.InvariantCulture) &&
                               FullNamePath[3].StartsWith(CulturePattern, StringComparison.InvariantCulture) &&
                               FullNamePath[4].StartsWith(PublicKeyTokenPattern, StringComparison.InvariantCulture);

            // Name should be always valid for a type name unless .NET changes drastically.
            if (!IsValidName)
                throw new ArgumentException("Assembly qualified type name expected", nameof(name));

            bool NamespaceMatch = MatchNamespace(FullNamePath[0], descriptorSearch.Path, descriptorReplace.Path, out string namespaceOverride);
            bool AssemblyMatch = MatchWithPattern(FullNamePath[1], AssemblyPattern, descriptorSearch.Assembly, descriptorReplace.Assembly, out string assemblyOverride);
            bool VersionMatch = MatchWithPattern(FullNamePath[2], VersionPattern, descriptorSearch.Version, descriptorReplace.Version, out string versionOverride);
            bool CultureMatch = MatchWithPattern(FullNamePath[3], CulturePattern, descriptorSearch.Culture, descriptorReplace.Culture, out string cultureOverride);
            bool PublicKeyTokenMatch = MatchWithPattern(FullNamePath[4], PublicKeyTokenPattern, descriptorSearch.PublicKeyToken, descriptorReplace.PublicKeyToken, out string publicKeyTokenOverride);

            bool IsMatch = NamespaceMatch && AssemblyMatch && VersionMatch && CultureMatch && PublicKeyTokenMatch;
            if (IsMatch)
                nameOverride = $"{namespaceOverride},{assemblyOverride},{versionOverride},{cultureOverride},{publicKeyTokenOverride}";
            else
                nameOverride = name;

            return IsMatch;
        }

        private static bool MatchNamespace(string pathText, string searchPath, string replacePath, out string pathOverride)
        {
            pathOverride = string.Empty;
            bool NamespaceMatch = false;

            string[] NamePath = pathText.Split('.');
            Debug.Assert(NamePath.Length > 0);

            for (int i = NamePath.Length; i > 0 && !NamespaceMatch; i--)
            {
                string PartialNamespace = string.Empty;
                for (int j = 0; j + 1 < i; j++)
                {
                    if (PartialNamespace.Length > 0)
                        PartialNamespace += ".";
                    PartialNamespace += NamePath[j];
                }

                NamespaceMatch = StringMatch(PartialNamespace, searchPath, replacePath, out pathOverride);
            }

            if (NamespaceMatch)
            {
                Debug.Assert(pathOverride.Length > 0);
                pathOverride = pathOverride + "." + NamePath[NamePath.Length - 1];
            }
            else
                pathOverride = pathText;

            return NamespaceMatch;
        }

        private static bool MatchWithPattern(string text, string pattern, string searchText, string replaceText, out string textOverride)
        {
            bool Result = StringMatch(text.Substring(pattern.Length), searchText, replaceText, out textOverride);

            textOverride = pattern + textOverride;
            return Result;
        }

        private static bool StringMatch(string text, string searchText, string replaceText, out string textOverride)
        {
            if (searchText == null)
            {
                textOverride = text;
                return true;
            }

            if (searchText == text || searchText == MatchAny)
            {
                textOverride = replaceText;
                return true;
            }

            textOverride = text;
            return false;
        }

        /// <summary>
        /// Fills a descriptor with information from a type.
        /// </summary>
        /// <param name="type">The type to read.</param>
        public static NamespaceDescriptor DescriptorFromType(Type type)
        {
            return new NamespaceDescriptor() { Path = PathFromType(type), Assembly = AssemblyFromType(type), Version = VersionFromType(type), Culture = CultureFromType(type), PublicKeyToken = PublicKeyTokenFromType(type) };
        }

        /// <summary>
        /// Gets the path subset of a descriptor from information from a type.
        /// </summary>
        /// <param name="type">The type to read.</param>
        public static string PathFromType(Type type)
        {
            string FullName = type.AssemblyQualifiedName;

            string[] FullNamePath = FullName.Split(',');
            Debug.Assert(FullNamePath.Length == 5);

            string[] NamePath = FullNamePath[0].Split('.');

            string Result = string.Empty;
            for (int i = 0; i + 1 < NamePath.Length; i++)
            {
                if (Result.Length > 0)
                    Result += ".";

                Result += NamePath[i];
            }

            return Result;
        }

        /// <summary>
        /// Gets the assembly subset of a descriptor from information from a type.
        /// </summary>
        /// <param name="type">The type to read.</param>
        public static string AssemblyFromType(Type type)
        {
            string FullName = type.AssemblyQualifiedName;

            string[] FullNamePath = FullName.Split(',');
            Debug.Assert(FullNamePath.Length == 5);

            return FullNamePath[1].Trim();
        }

        /// <summary>
        /// Gets the version subset of a descriptor from information from a type.
        /// </summary>
        /// <param name="type">The type to read.</param>
        public static string VersionFromType(Type type)
        {
            return TextWithPatternFromType(type, VersionPattern);
        }

        /// <summary>
        /// Gets the culture subset of a descriptor from information from a type.
        /// </summary>
        /// <param name="type">The type to read.</param>
        public static string CultureFromType(Type type)
        {
            return TextWithPatternFromType(type, CulturePattern);
        }

        /// <summary>
        /// Gets the public key token subset of a descriptor from information from a type.
        /// </summary>
        /// <param name="type">The type to read.</param>
        public static string PublicKeyTokenFromType(Type type)
        {
            return TextWithPatternFromType(type, PublicKeyTokenPattern);
        }

        private static string TextWithPatternFromType(Type type, string pattern)
        {
            string Result = null;

            string FullName = type.AssemblyQualifiedName;
            int StartIndex = FullName.LastIndexOf(pattern);
            Debug.Assert(StartIndex >= 0);

            if (StartIndex >= 0)
            {
                string EndName = FullName.Substring(StartIndex + pattern.Length);
                int EndIndex = EndName.IndexOf(",");
                if (EndIndex < 0)
                    EndIndex = EndName.Length;

                if (EndIndex >= 0)
                    Result = EndName.Substring(0, EndIndex);
            }

            return Result;
        }
        #endregion

        #region Debugging
        /// <summary>
        /// Returns a string representing the instance, for debug purpose.
        /// </summary>
        public override string ToString()
        {
            return $"'{Path}, {Assembly}, {Version}, {Culture}, {PublicKeyToken}'";
        }
        #endregion
    }
}
