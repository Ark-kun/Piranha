using Ark.Linq;
using Mono.Cecil;
using System.Collections.Generic;
using System.IO;

namespace Ark.Cecil {
    public abstract class CecilProcessor {
        public void ProcessAssemblyFromFile(string inputFile, string outputFile) {
            string inputFileBase = Path.Combine(Path.GetDirectoryName(inputFile), Path.GetFileNameWithoutExtension(inputFile));
            bool hasSymbols = File.Exists(inputFileBase + ".pdb") || File.Exists(inputFileBase + ".mdb");
            ProcessAssemblyFromFile(inputFile, new ReaderParameters() { ReadSymbols = hasSymbols }, outputFile, new WriterParameters() { WriteSymbols = hasSymbols });
        }

        public void ProcessAssemblyFromFile(string inputFile, ReaderParameters readerParams, string outputFile, WriterParameters writerParams) {
            Stream inputStream = File.OpenRead(inputFile);
            Stream outputStream = outputFile == null ? null : File.Create(outputFile);
            ProcessAssemblyFromStream(inputStream, readerParams, outputStream, writerParams);
        }

        public void ProcessAssemblyFromStream(Stream inputStream, Stream outputStream) {
            ProcessAssemblyFromStream(inputStream, new ReaderParameters(), outputStream, new WriterParameters());
        }

        public void ProcessAssemblyFromStream(Stream inputStream, ReaderParameters readerParams, Stream outputStream, WriterParameters writerParams) {
            var assemblyDef = AssemblyDefinition.ReadAssembly(inputStream, readerParams);
            ProcessAssembly(assemblyDef);
            if (outputStream != null) {
                assemblyDef.Write(outputStream, writerParams);
            }
        }

        public virtual void ProcessAssembly(AssemblyDefinition assemblyDef) {
            ProcessModules(assemblyDef, assemblyDef.Modules);
            ProcessCustomAssemblyAttributes(assemblyDef, assemblyDef.CustomAttributes);
        }

        public virtual void ProcessCustomAssemblyAttributes(AssemblyDefinition assemblyDef, IList<CustomAttribute> attributes) {
            attributes.ForEach(ProcessCustomAssemblyAttribute);
        }

        public virtual void ProcessCustomAssemblyAttribute(CustomAttribute attribute) { }

        public virtual void ProcessModules(AssemblyDefinition assemblyDef, IList<ModuleDefinition> moduleDefs) {
            moduleDefs.ForEach(ProcessModule);
        }

        public virtual void ProcessModule(ModuleDefinition moduleDef) {
            ProcessCustomModuleAttributes(moduleDef, moduleDef.CustomAttributes);
            ProcessAssemblyReferences(moduleDef, moduleDef.AssemblyReferences);
            ProcessModuleReferences(moduleDef, moduleDef.ModuleReferences);
            ProcessModuleTypes(moduleDef, moduleDef.Types);
            ProcessExportedTypes(moduleDef, moduleDef.ExportedTypes);
            ProcessResources(moduleDef, moduleDef.Resources);
        }
        
        public virtual void ProcessCustomModuleAttributes(ModuleDefinition moduleDef, IList<CustomAttribute> attributes) {
            attributes.ForEach(ProcessCustomModuleAttribute);
        }

        public virtual void ProcessCustomModuleAttribute(CustomAttribute attribute) { }

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
            ProcessCustomTypeAttributes(typeDef, typeDef.CustomAttributes);
            ProcessFields(typeDef, typeDef.Fields);
            ProcessMethods(typeDef, typeDef.Methods);
            ProcessProperties(typeDef, typeDef.Properties);
            ProcessEvents(typeDef, typeDef.Events);
        }

        public virtual void ProcessCustomTypeAttributes(TypeDefinition typeDef, IList<CustomAttribute> attributes) {
            attributes.ForEach(ProcessCustomTypeAttribute);
        }

        public virtual void ProcessCustomTypeAttribute(CustomAttribute attribute) { }

        public virtual void ProcessFields(TypeDefinition typeDef, IList<FieldDefinition> fieldDefs) {
            fieldDefs.ForEach(ProcessField);
        }

        public virtual void ProcessField(FieldDefinition fieldDef) {
            ProcessCustomFieldAttributes(fieldDef, fieldDef.CustomAttributes);
        }

        public virtual void ProcessCustomFieldAttributes(FieldDefinition fieldDef, IList<CustomAttribute> attributes) {
            attributes.ForEach(ProcessCustomFieldAttribute);
        }

        public virtual void ProcessCustomFieldAttribute(CustomAttribute attribute) { }

        public virtual void ProcessProperties(TypeDefinition typeDef, IList<PropertyDefinition> propertyDefs) {
            propertyDefs.ForEach(ProcessProperty);
        }

        public virtual void ProcessProperty(PropertyDefinition propertyDef) {
            ProcessCustomPropertyAttributes(propertyDef, propertyDef.CustomAttributes);
        }

        public virtual void ProcessCustomPropertyAttributes(PropertyDefinition propertyDef, IList<CustomAttribute> attributes) {
            attributes.ForEach(ProcessCustomPropertyAttribute);
        }

        public virtual void ProcessCustomPropertyAttribute(CustomAttribute attribute) { }

        public virtual void ProcessEvents(TypeDefinition typeDef, IList<EventDefinition> eventDefs) {
            eventDefs.ForEach(ProcessEvent);
        }

        public virtual void ProcessEvent(EventDefinition eventDef) {
            ProcessCustomEventAttributes(eventDef, eventDef.CustomAttributes);
        }

        public virtual void ProcessCustomEventAttributes(EventDefinition eventDef, IList<CustomAttribute> attributes) {
            attributes.ForEach(ProcessCustomEventAttribute);
        }

        public virtual void ProcessCustomEventAttribute(CustomAttribute attribute) { }

        public virtual void ProcessMethods(TypeDefinition typeDef, IList<MethodDefinition> methodDefs) {
            methodDefs.ForEach(ProcessMethod);
        }

        public virtual void ProcessMethod(MethodDefinition methodDef) {
            ProcessCustomMethodAttributes(methodDef, methodDef.CustomAttributes);
        }

        public virtual void ProcessCustomMethodAttributes(MethodDefinition methodDef, IList<CustomAttribute> attributes) {
            attributes.ForEach(ProcessCustomMethodAttribute);
        }

        public virtual void ProcessCustomMethodAttribute(CustomAttribute attribute) { }
    }
}
