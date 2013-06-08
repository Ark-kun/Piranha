using Ark.Linq;
using Mono.Cecil;
using System.Collections.Generic;
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
            ProcessAssembly(assemblyDef);
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

        public virtual void ProcessAssembly(AssemblyDefinition assemblyDef) {
            ProcessModules(assemblyDef, assemblyDef.Modules);
            ProcessCustomAttributes(assemblyDef.CustomAttributes, assemblyDef);
        }

        public virtual void ProcessModules(AssemblyDefinition assemblyDef, IList<ModuleDefinition> moduleDefs) {
            moduleDefs.ForEach(ProcessModule);
        }

        public virtual void ProcessModule(ModuleDefinition moduleDef) {
            ProcessAssemblyReferences(moduleDef, moduleDef.AssemblyReferences);
            ProcessModuleReferences(moduleDef, moduleDef.ModuleReferences);
            ProcessCustomAttributes(moduleDef.CustomAttributes, moduleDef);
            ProcessModuleTypes(moduleDef, moduleDef.Types);
            ProcessExportedTypes(moduleDef, moduleDef.ExportedTypes);
            ProcessResources(moduleDef, moduleDef.Resources);
        }

        public virtual void ProcessAssemblyReferences(ModuleDefinition moduleDef, IList<AssemblyNameReference> assemblyNameRefs) {
            assemblyNameRefs.ForEach(ProcessAssemblyReference);
        }

        public virtual void ProcessAssemblyReference(AssemblyNameReference assemblyNameRef) { }

        public virtual void ProcessModuleReferences(ModuleDefinition moduleDef, IList<ModuleReference> moduleRefs) {
            moduleRefs.ForEach(ProcessModuleReference);
        }

        public virtual void ProcessModuleReference(ModuleReference moduleRef) { }

        public virtual void ProcessResources(ModuleDefinition moduleDef, IList<Resource> resources) {
            resources.ForEach(ProcessResource);
        }

        public virtual void ProcessResource(Resource resource) { }

        public virtual void ProcessExportedTypes(ModuleDefinition moduleDef, IList<ExportedType> exportedTypes) {
            exportedTypes.ForEach(ProcessExportedType);
        }

        public virtual void ProcessExportedType(ExportedType exportedType) { }

        public virtual void ProcessModuleTypes(ModuleDefinition moduleDef, IList<TypeDefinition> typeDefs) {
            typeDefs.ForEach(ProcessTypeAndNestedTypes);
        }

        public virtual void ProcessTypeAndNestedTypes(TypeDefinition typeDef) {
            ProcessNestedTypes(typeDef, typeDef.NestedTypes);
            ProcessType(typeDef);
        }

        public virtual void ProcessNestedTypes(TypeDefinition typeDef, IList<TypeDefinition> typeDefs) {
            typeDefs.ForEach(ProcessTypeAndNestedTypes);
        }

        public virtual void ProcessType(TypeDefinition typeDef) {
            ProcessCustomAttributes(typeDef.CustomAttributes, typeDef);
            ProcessFields(typeDef, typeDef.Fields);
            ProcessMethods(typeDef, typeDef.Methods);
            ProcessProperties(typeDef, typeDef.Properties);
            ProcessEvents(typeDef, typeDef.Events);
        }

        public virtual void ProcessFields(TypeDefinition typeDef, IList<FieldDefinition> fieldDefs) {
            fieldDefs.ForEach(ProcessField);
        }

        public virtual void ProcessField(FieldDefinition fieldDef) {
            ProcessCustomAttributes(fieldDef.CustomAttributes, fieldDef);
        }

        public virtual void ProcessProperties(TypeDefinition typeDef, IList<PropertyDefinition> propertyDefs) {
            propertyDefs.ForEach(ProcessProperty);
        }

        public virtual void ProcessProperty(PropertyDefinition propertyDef) {
            ProcessCustomAttributes(propertyDef.CustomAttributes, propertyDef);
        }

        public virtual void ProcessEvents(TypeDefinition typeDef, IList<EventDefinition> eventDefs) {
            eventDefs.ForEach(ProcessEvent);
        }

        public virtual void ProcessEvent(EventDefinition eventDef) {
            ProcessCustomAttributes(eventDef.CustomAttributes, eventDef);
        }

        public virtual void ProcessMethods(TypeDefinition typeDef, IList<MethodDefinition> methodDefs) {
            methodDefs.ForEach(ProcessMethod);
        }

        public virtual void ProcessMethod(MethodDefinition methodDef) {
            ProcessCustomAttributes(methodDef.CustomAttributes, methodDef);
        }

        public virtual void ProcessCustomAttributes(IList<CustomAttribute> attributes, IMetadataTokenProvider owner) {
            attributes.ForEach(attr => ProcessCustomAttribute(attr, owner));
        }

        public virtual void ProcessCustomAttribute(CustomAttribute attribute, IMetadataTokenProvider owner) { }
    }
}
