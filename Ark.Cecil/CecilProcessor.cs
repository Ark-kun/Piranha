using Ark.Linq;
using Mono.Cecil;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Ark.Cecil {
    public abstract class CecilProcessor {
        public void ProcessAssemblyFromFile(string inputFile, string outputFile) {
            string inputFileBase = Path.Combine(Path.GetDirectoryName(inputFile), Path.GetFileNameWithoutExtension(inputFile));
            bool hasSymbols = File.Exists(inputFileBase + ".pdb") || File.Exists(inputFileBase + ".mdb");
            var readerParameters = GetDefaultReaderParameters();
            var writerParameters = GetDefaultWriterParameters();
            readerParameters.ReadSymbols = hasSymbols;
            writerParameters.WriteSymbols = hasSymbols;
            ProcessAssemblyFromFile(inputFile, readerParameters, outputFile, writerParameters);
        }

        public void ProcessAssemblyFromFile(string inputFile, ReaderParameters readerParams, string outputFile, WriterParameters writerParams) {
            Stream inputStream = File.OpenRead(inputFile);
            Stream outputStream = outputFile == null ? null : File.Create(outputFile);
            ProcessAssemblyFromStream(inputStream, readerParams, outputStream, writerParams);
        }

        public void ProcessAssemblyFromStream(Stream inputStream, Stream outputStream) {
            ProcessAssemblyFromStream(inputStream, GetDefaultReaderParameters(), outputStream, GetDefaultWriterParameters());
        }

        public void ProcessAssemblyFromStream(Stream inputStream, ReaderParameters readerParams, Stream outputStream, WriterParameters writerParams) {
            var assemblyDef = AssemblyDefinition.ReadAssembly(inputStream, readerParams);
            Process(assemblyDef);
            if (outputStream != null) {
                assemblyDef.Write(outputStream, writerParams);
            }
        }

        protected virtual ReaderParameters GetDefaultReaderParameters() {
            return new ReaderParameters();
        }

        protected virtual WriterParameters GetDefaultWriterParameters() {
            return new WriterParameters();
        }

        protected virtual void BeforeProcessing() {
            var longName = this.GetType().Name;
            var shortName = longName;
            if (shortName.EndsWith("Processor")) {
                shortName.Remove(shortName.Length - "Processor".Length);
            }
            Trace.WriteLine(string.Format("Started {0}.", longName), shortName);
            Trace.Indent();
        }

        protected virtual void AfterProcessing() {
            var longName = this.GetType().Name;
            var shortName = longName;
            if (shortName.EndsWith("Processor")) {
                shortName.Remove(shortName.Length - "Processor".Length);
            }
            Trace.Unindent();
            Trace.WriteLine(string.Format("Finished {0}.", longName), shortName);
        }

        public void Process(AssemblyDefinition assemblyDef) {
            BeforeProcessing();
            ProcessAssembly(assemblyDef);
            AfterProcessing();
        }

        protected virtual void ProcessAssembly(AssemblyDefinition assemblyDef) {
            ProcessModules(assemblyDef, assemblyDef.Modules);
            ProcessCustomAttributes(assemblyDef.CustomAttributes, assemblyDef);
        }

        protected virtual void ProcessModules(AssemblyDefinition assemblyDef, IList<ModuleDefinition> moduleDefs) {
            moduleDefs.ReversedForEach(ProcessModule);
        }

        protected virtual void ProcessModule(ModuleDefinition moduleDef) {
            ProcessAssemblyReferences(moduleDef, moduleDef.AssemblyReferences);
            ProcessModuleReferences(moduleDef, moduleDef.ModuleReferences);
            ProcessCustomAttributes(moduleDef.CustomAttributes, moduleDef);
            ProcessModuleTypes(moduleDef, moduleDef.Types);
            ProcessExportedTypes(moduleDef, moduleDef.ExportedTypes);
            ProcessResources(moduleDef, moduleDef.Resources);
        }

        protected virtual void ProcessAssemblyReferences(ModuleDefinition moduleDef, IList<AssemblyNameReference> assemblyNameRefs) {
            assemblyNameRefs.ReversedForEach(ProcessAssemblyReference);
        }

        protected virtual void ProcessAssemblyReference(AssemblyNameReference assemblyNameRef) { }

        protected virtual void ProcessModuleReferences(ModuleDefinition moduleDef, IList<ModuleReference> moduleRefs) {
            moduleRefs.ReversedForEach(ProcessModuleReference);
        }

        protected virtual void ProcessModuleReference(ModuleReference moduleRef) { }

        protected virtual void ProcessResources(ModuleDefinition moduleDef, IList<Resource> resources) {
            resources.ReversedForEach(ProcessResource);
        }

        protected virtual void ProcessResource(Resource resource) { }

        protected virtual void ProcessExportedTypes(ModuleDefinition moduleDef, IList<ExportedType> exportedTypes) {
            exportedTypes.ReversedForEach(ProcessExportedType);
        }

        protected virtual void ProcessExportedType(ExportedType exportedType) { }

        protected virtual void ProcessModuleTypes(ModuleDefinition moduleDef, IList<TypeDefinition> typeDefs) {
            typeDefs.ReversedForEach(ProcessTypeAndNestedTypes);
        }

        protected virtual void ProcessTypeAndNestedTypes(TypeDefinition typeDef) {
            ProcessNestedTypes(typeDef, typeDef.NestedTypes);
            ProcessType(typeDef);
        }

        protected virtual void ProcessNestedTypes(TypeDefinition typeDef, IList<TypeDefinition> typeDefs) {
            typeDefs.ReversedForEach(ProcessTypeAndNestedTypes);
        }

        protected virtual void ProcessType(TypeDefinition typeDef) {
            ProcessCustomAttributes(typeDef.CustomAttributes, typeDef);
            ProcessFields(typeDef, typeDef.Fields);
            ProcessMethods(typeDef, typeDef.Methods);
            ProcessProperties(typeDef, typeDef.Properties);
            ProcessEvents(typeDef, typeDef.Events);
        }

        protected virtual void ProcessFields(TypeDefinition typeDef, IList<FieldDefinition> fieldDefs) {
            fieldDefs.ReversedForEach(ProcessField);
        }

        protected virtual void ProcessField(FieldDefinition fieldDef) {
            ProcessCustomAttributes(fieldDef.CustomAttributes, fieldDef);
        }

        protected virtual void ProcessProperties(TypeDefinition typeDef, IList<PropertyDefinition> propertyDefs) {
            propertyDefs.ReversedForEach(ProcessProperty);
        }

        protected virtual void ProcessProperty(PropertyDefinition propertyDef) {
            ProcessCustomAttributes(propertyDef.CustomAttributes, propertyDef);
        }

        protected virtual void ProcessEvents(TypeDefinition typeDef, IList<EventDefinition> eventDefs) {
            eventDefs.ReversedForEach(ProcessEvent);
        }

        protected virtual void ProcessEvent(EventDefinition eventDef) {
            ProcessCustomAttributes(eventDef.CustomAttributes, eventDef);
        }

        protected virtual void ProcessMethods(TypeDefinition typeDef, IList<MethodDefinition> methodDefs) {
            methodDefs.ReversedForEach(ProcessMethod);
        }

        protected virtual void ProcessMethod(MethodDefinition methodDef) {
            ProcessCustomAttributes(methodDef.CustomAttributes, methodDef);
        }

        protected virtual void ProcessCustomAttributes(IList<CustomAttribute> attributes, ICustomAttributeProvider owner) {
            attributes.ReversedForEach(attr => ProcessCustomAttribute(attr, owner));
        }

        protected virtual void ProcessCustomAttribute(CustomAttribute attribute, ICustomAttributeProvider owner) { }
    }
}
