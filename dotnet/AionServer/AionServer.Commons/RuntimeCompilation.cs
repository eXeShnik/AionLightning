// namespace AionServer.Commons;

// // 1
// DirectoryInfo d = new DirectoryInfo(sourceFilesLocation); 
// string[] sourceFiles = d.EnumerateFiles("\*.cs", SearchOption.AllDirectories)                
//     .Select(a => a.FullName).ToArray();
//             
// // 2
// List<SyntaxTree> trees = new List<SyntaxTree>();
// foreach (string file in sourceFiles)
// {
//     string code = File.ReadAllText(file);
//     SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
//     trees.Add(tree);
// }
//             
// // 3
// MetadataReference mscorlib =
//     MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
// MetadataReference codeAnalysis =
//     MetadataReference.CreateFromFile(typeof(SyntaxTree).Assembly.Location);
// MetadataReference csharpCodeAnalysis =
//     MetadataReference.CreateFromFile(typeof(CSharpSyntaxTree).Assembly.Location);
//
// MetadataReference[] references = { mscorlib, codeAnalysis, csharpCodeAnalysis };
//
// // 4
// var compilation = CSharpCompilation.Create("qwerty.dll",
//     trees,
//     references,
//     new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
// var result = compilation.Emit(Path.Combine(destinationLocation, "qwerty.dll"));