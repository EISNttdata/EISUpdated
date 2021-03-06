namespace EaiConverter.CodeGenerator
{
    using System;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.IO;

    using EaiConverter.Builder.Utils;
    using EaiConverter.Processor;

    using log4net;

    public class CsharpSourceCodeGeneratorService : ISourceCodeGeneratorService
    {
        // TODO VC : Put the output directory as a parameter";
        public const string SolutionDestinationPath = "./GeneratedSolution";

        public const string ProjectDestinationPath = "./GeneratedSolution/GeneratedSolution";

        private const string LineToReplace = "    <!-- insert file Generated here -->";

        private static readonly ILog Log = LogManager.GetLogger(typeof(CsharpSourceCodeGeneratorService));

        private int counter;
        private CodeDomProvider provider;
        private CodeGeneratorOptions options;
        private string fileExtension;

        public CsharpSourceCodeGeneratorService()
        {
            this.CreateSolutionDirectory();
            this.CreateSolutionAndProjectFiles();
            this.InitCodeDomProvider();
        }

        public void Generate(CodeCompileUnit targetUnit)
        {
            foreach (CodeNamespace codeNamespace in targetUnit.Namespaces)
            {
                var namespaceName = codeNamespace.Name;

                foreach (CodeTypeDeclaration codeType in codeNamespace.Types)
                {
                    string filename = codeType.Name + this.fileExtension;

                    string sourceFile = this.PathFromNamespace(ProjectDestinationPath, namespaceName) + GetFileSeparator() + filename;
                    if (File.Exists(sourceFile))
                    {
                        Log.Warn("############## Warning" + sourceFile + " has already been generated ");

                        // TODO add counter to fiel name
                        this.counter++;
                        filename = codeType.Name + this.counter + this.fileExtension;
                        sourceFile = this.PathFromNamespace(ProjectDestinationPath, namespaceName) + GetFileSeparator() + filename;
                    }

                    var newCodeNamespace = new CodeNamespace(codeNamespace.Name);
                    newCodeNamespace.Types.Add(codeType);
                    foreach (CodeNamespaceImport import in codeNamespace.Imports)
                    {
                        newCodeNamespace.Imports.Add(import);
                    }

                    using (var sw = new StreamWriter(sourceFile, false))
                    {
                        var tw = new IndentedTextWriter(sw, "    ");
                        this.provider.GenerateCodeFromNamespace(newCodeNamespace, tw, this.options);
                        tw.Close();
                    }

                    this.AddFileToCsproj(namespaceName, filename);

                    Log.Info(sourceFile + " has been generated");
                }
            }
        }

        private void CreateSolutionDirectory()
        {
            if (Directory.Exists(SolutionDestinationPath)
                && ConfigurationApp.GetProperty("IsGeneratedSolutionDirectoryPurged") != "true")
            {
                Directory.Delete(SolutionDestinationPath, true);
                ConfigurationApp.SaveProperty("IsGeneratedSolutionDirectoryPurged", "true");
            }

            Directory.CreateDirectory(SolutionDestinationPath);
            Directory.CreateDirectory(ProjectDestinationPath);
            Directory.CreateDirectory(ProjectDestinationPath + "/Properties");
            Directory.CreateDirectory(ProjectDestinationPath + "/" + ConvertNamespaceToPath(TargetAppNameSpaceService.xmlToolsNameSpace()));
        }

        private void CreateSolutionAndProjectFiles()
        {
            using (var file = new StreamWriter(SolutionDestinationPath + "/GeneratedSolution.sln"))
            {
                file.Write(GeneratedSolution_sln);
            }

            using (var file = new StreamWriter(ProjectDestinationPath + "/GeneratedSolution.csproj"))
            {
                file.Write(GeneratedSolution_csproj);
            }

            using (var file = new StreamWriter(ProjectDestinationPath + "/Properties/AssemblyInfo.cs"))
            {
                file.Write(AssemblyInfo_cs);
            }

            using (var file = new StreamWriter(ProjectDestinationPath + GetFileSeparator() + ConvertNamespaceToPath(TargetAppNameSpaceService.xmlToolsNameSpace()) + "/TibcoXslHelper.cs"))
            {
                file.Write(TibcoXslHelper_cs);
            }
        }

        private void InitCodeDomProvider()
        {
            this.provider = CodeDomProvider.CreateProvider("CSharp");
            this.options = new CodeGeneratorOptions();
            this.options.BracingStyle = "C";
            this.options.IndentString = "    ";
            this.options.BlankLinesBetweenMembers = true;
            if (this.provider.FileExtension[0] == '.')
            {
                this.fileExtension = this.provider.FileExtension;
            }
            else
            {
                this.fileExtension = "." + this.provider.FileExtension;
            }
        }

        private void AddFileToCsproj(string namespaceName, string filename)
        {
            string relativeSourceFile = ConvertNamespaceToPath(namespaceName) + GetFileSeparator() + filename;
            if (File.Exists(ProjectDestinationPath + "/GeneratedSolution.csproj"))
            {
                string projectFile = File.ReadAllText(ProjectDestinationPath + "/GeneratedSolution.csproj");
                projectFile = projectFile.Replace(LineToReplace, LineToReplace + "\n" + "    <Compile Include=\"" + relativeSourceFile + "\"/>");
                File.WriteAllText(ProjectDestinationPath + "/GeneratedSolution.csproj", projectFile);
            }
        }

        // TODO refactor because not really SRP
        private string PathFromNamespace(string outputPath, string ns)
        {
            var path = string.Format("{0}/{1}", outputPath, ConvertNamespaceToPath(ns));

            Directory.CreateDirectory(path);
            return path;
        }

        private static string ConvertNamespaceToPath(string ns)
        {
            var path = ns.Replace('.', GetFileSeparator().ToCharArray()[0]);
            return path;
        }

        private static string GetFileSeparator()
        {
            if (Environment.OSVersion.ToString().Contains("indows"))
            {
                return "\\";
            }
            else
            {
                return "/";
            }
        }


        public static string GeneratedSolution_sln = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 2012
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""GeneratedSolution"", ""GeneratedSolution\GeneratedSolution.csproj"", ""{35F53A37-C2B2-41CE-BB32-F6AD0C862E0F}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{35F53A37-C2B2-41CE-BB32-F6AD0C862E0F}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{35F53A37-C2B2-41CE-BB32-F6AD0C862E0F}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{35F53A37-C2B2-41CE-BB32-F6AD0C862E0F}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{35F53A37-C2B2-41CE-BB32-F6AD0C862E0F}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal

";


        public static string GeneratedSolution_csproj = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{35F53A37-C2B2-41CE-BB32-F6AD0C862E0F}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>GeneratedSolution</RootNamespace>
    <AssemblyName>GeneratedSolution</AssemblyName>
    <TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"" />
    <Reference Include=""System.Xml.Linq"" />
    <Reference Include=""System.Data.DataSetExtensions"" />
    <Reference Include=""Microsoft.CSharp"" />
    <Reference Include=""System.Data"" />
    <Reference Include=""System.Xml"" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""" + ConvertNamespaceToPath(TargetAppNameSpaceService.xmlToolsNameSpace()) + @"\TibcoXslHelper.cs"" />
    <Compile Include=""Properties\AssemblyInfo.cs"" />
    <!-- insert file Generated here -->
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
  <!-- To modify your build process, add your task inside one of the targets below and uncomment it. 
       Other similar extension points exist, see Microsoft.Common.targets.
  <Target Name=""BeforeBuild"">
  </Target>
  <Target Name=""AfterBuild"">
  </Target>
  -->
</Project>
";

        private static string AssemblyInfo_cs = @"using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle(""GeneratedSolution"")]
[assembly: AssemblyDescription("""")]
[assembly: AssemblyConfiguration("""")]
[assembly: AssemblyCompany(""Microsoft"")]
[assembly: AssemblyProduct(""GeneratedSolution"")]
[assembly: AssemblyCopyright(""Copyright � Microsoft 2015"")]
[assembly: AssemblyTrademark("""")]
[assembly: AssemblyCulture("""")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid(""6e10df1e-7154-44be-9dc1-c95a47c19670"")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion(""1.0.*"")]
[assembly: AssemblyVersion(""1.0.0.0"")]
[assembly: AssemblyFileVersion(""1.0.0.0"")]
";

        public static string TibcoXslHelper_cs = "namespace " + TargetAppNameSpaceService.myAppName() + @".Tools.Xml
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Xml.Serialization;

    public static class TibcoXslHelper
    {
        public static DateTime ParseDateTime(string format, string inputDate)
        {
            return DateTime.ParseExact(inputDate, format, null);
        }

        public static string FormatDateTime(string format, DateTime inputDate)
        {
            return string.Format(format, inputDate);
        }

        public static DateTime AddToDate(DateTime inputDate, int yearToAdd, int monthToAdd, int dayToAdd)
        {
            inputDate.AddYears(yearToAdd);
            inputDate.AddMonths(monthToAdd);
            return inputDate.AddDays(dayToAdd);
        }

        public static double ParseNumber(string numberInString)
        {
            return double.Parse(numberInString);
        }
            
        public static string Concat(params object[] list)
        {
            return string.Concat(list);
        }

        public static int Count(IList myCollection)
        {
            return myCollection.Count;
        }

        public static bool StartsWith(string myString, string prefixToCheck)
        {
            return myString.StartsWith(prefixToCheck);
        }
    
        public static bool Contains(string value, string inputString)
        {
            return inputString.Contains(value);
        }

        // usage of exists : exists ('this string', mycollection)
        public static bool Exist<T>(T value, List<T> collection)
        {
            return collection.Contains(value);
        }

        // usage of translate : translate (myvaraible/value, '&#xA;', '')
        public static string Translate(string inputString, string oldstring, string newstring)
        {
            return inputString.Replace(oldstring, newstring);
        }

        // usage of string-length : string-length (myvariable)
        public static int StringLength(string inputString)
        {
            return inputString.Length;
        }

        // usage of Left : left(myvariable,3)
        public static string Left(string inputString, int length)
        {
            return inputString.Substring (0, length);
        }

        // usage of substring : substring(myvariable,3,5)
        public static string Substring(string inputString, int startindex, int endindex)
        {
            return inputString.Substring (startindex-1, endindex-1);
        }

        public static int IndexOf(string inputString, string stringToFind)
        {
            return inputString.IndexOf(stringToFind);
        }

        public static string RenderXml(object dataToSerialize)
        {
            return RenderXml(dataToSerialize, true);
        }

        //usage a string, usage sample : tib:render-xml(myvariable, true()) 
        public static string RenderXml(object dataToSerialize, bool isSomething)
        {
            if (dataToSerialize == null)
            {
                return null;
            }

            using (StringWriter stringwriter = new System.IO.StringWriter())
            {
                var serializer = new XmlSerializer(dataToSerialize.GetType());
                serializer.Serialize(stringwriter, dataToSerialize);
                return stringwriter.ToString();
            }
        }

        public static int Round(int nb)
        {
            return nb;
        }

        public static int Round(double nb)
        {
            return Convert.ToInt32(Math.Round(nb));
        }

        public static int Round(decimal nb)
        {
            return Convert.ToInt32(Math.Round(nb));
        }

        // usage tib:trim : tib:trim(myvariable)
        public static string Trim(string inputString)
        {
            return inputString.Trim();
        }

        // usage tib:tokenize : tib:tokenize(""tot1;tot2"", "";"")
        public static string[] Tokenize(string inputString, string delimiter)
        {
            return inputString.Split(delimiter.ToCharArray()[0]);
        }

        // usage tib:translate-timezone( : tib:translate-timezone(date, ""UTC"")
        // TODO find usage exemple
        public static DateTime TranslateTimezone(DateTime date, string timezone)
        {
            TimeZoneInfo destinationTimeZone;
            if (timezone == ""UTC"")
            {
                destinationTimeZone = TimeZoneInfo.Utc;
            }
            else
            {
                throw new NotImplementedException();
            }
            return TimeZoneInfo.ConvertTime(date, destinationTimeZone);

        }

        // usage tib:compare-date( : tib:compare-date(date1, date2) , return 0 if equals
        //expression = expression.Replace(""tib:compare-date("",""TibcoXslHelper.CompareDate("");
        public static int CompareDate(DateTime date1, DateTime date2)
        {
            return date1.CompareTo(date2);
        }

        // usage upper-case : upper-case (mystring)
        public static string UpperCase(string inputString)
        {
            return inputString.ToUpper();
        }

        public static string LowerCase(string inputString)
        {
            return inputString.ToLower();
        }

        // tib:validate-dateTime(<<format>>, <<string>>) return bool
        public static bool ValidateDateTime(string format, string inputDate)
        {
            try
            {
                DateTime.ParseExact(inputDate, format, null);
            }
            catch (System.FormatException ex)
            {
                return false;
            }

            return true;
        }

        // return a string, usage sample : tib:string-round-fraction($Start/root/inputdata, 2)
        // for exemple tib:string-round-fraction(round(1.100), 2) Output as 1.00
        public static string StringRoundFraction(string myNumber, int nbDecimal)
        {
            return Math.Round(decimal.Parse(myNumber), nbDecimal).ToString();
        }
    }
}       
";
    }
}
