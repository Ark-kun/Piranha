using Ark.Cecil;
using Ark.Linq;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ark.Piranha {
    public abstract class TypeDependency : IEquatable<TypeDependency> {
        public abstract int Priority { get; }
        public abstract IMetadataTokenProvider DependingMember { get; }
        //public abstract TypeDefinition DependentType { get; }

        public virtual void Break() {
            Trace.WriteLine(String.Format("Breaking dependency of {0}.", DependingMember), "TypeDependency");
        }

        public bool Equals(TypeDependency other) {
            return (object)other != null && other.GetType() == this.GetType() && CecilEqualityComparer.AreEqual(other.DependingMember, this.DependingMember);
        }

        public override bool Equals(object obj) {
            var typeDependency = obj as TypeDependency;
            return typeDependency != null && this.Equals(typeDependency);
        }

        public override int GetHashCode() {
            return this.GetType().GetHashCode() ^ CecilEqualityComparer.GetHashCode(DependingMember);
        }

        public override string ToString() {
            return DependingMember.ToString();
        }

        public static bool operator ==(TypeDependency a, TypeDependency b) {
            return (object)a == null ? (object)b == null : a.Equals(b);
        }

        public static bool operator !=(TypeDependency a, TypeDependency b) {
            return !(a == b);
        }
    }

    class BaseClassDependency : TypeDependency {
        TypeDefinition _derivedTypeDef;

        public BaseClassDependency(TypeDefinition typeDef) {
            _derivedTypeDef = typeDef;
        }

        public TypeDefinition DerivedClass {
            get { return _derivedTypeDef; }
        }

        public override IMetadataTokenProvider DependingMember {
            get { return _derivedTypeDef; }
        }

        public override int Priority { get { return 6; } }

        //Cascading deletion maust be handled elsewhere since it can trigger breaking many other dependencies.
        public override void Break() {
            Trace.WriteLine(String.Format("Removing derived class {0} because its base class is being removed.", DerivedClass, DerivedClass), "TypeDependency");
            //Maybe just remove the inheritance? Probably no.
            if (DerivedClass.IsNested) {
                DerivedClass.DeclaringType.NestedTypes.Remove(DerivedClass); //??
            } else {
                DerivedClass.Module.Types.Remove(DerivedClass);
            }
        }
    }

    class InterfaceDependency : TypeDependency {
        TypeDefinition _implementationTypeDef;
        TypeReference _interfaceRef;

        public InterfaceDependency(TypeDefinition implementationTypeDef, TypeReference interfaceRef) {
            _implementationTypeDef = implementationTypeDef;
            _interfaceRef = interfaceRef;
        }

        public TypeDefinition Implementation {
            get { return _implementationTypeDef; }
        }

        public TypeReference Interface {
            get { return _interfaceRef; }
        }

        public override IMetadataTokenProvider DependingMember {
            get { return _implementationTypeDef; }
        }

        public override int Priority { get { return 5; } }

        public override void Break() {
            Trace.WriteLine(String.Format("Removing the record of class {0} implementing interface {1} because the interface is being removed.", Implementation, Interface), "TypeDependency");
            Implementation.Interfaces.Remove(Interface);
        }
    }

    class FieldDependency : TypeDependency {
        FieldDefinition _fieldDef;

        public FieldDependency(FieldDefinition fieldDef) {
            _fieldDef = fieldDef;
        }

        public FieldDefinition Field {
            get { return _fieldDef; }
        }

        public override IMetadataTokenProvider DependingMember {
            get { return Field; }
        }

        public override int Priority { get { return 4; } }

        public override void Break() {
            Trace.WriteLine(String.Format("Removing field {0} because a type that it depends on is being removed.", Field), "TypeDependency");
            Field.DeclaringType.Fields.Remove(Field);
        }
    }

    class MethodDependency : TypeDependency {
        MethodDefinition _methodDef;

        public MethodDependency(MethodDefinition methodDef) {
            _methodDef = methodDef;
        }

        public MethodDefinition Method {
            get { return _methodDef; }
        }

        public override IMetadataTokenProvider DependingMember {
            get { return _methodDef; }
        }

        public override int Priority { get { return 3; } }

        public override void Break() {
            Trace.WriteLine(String.Format("Removing method {0} because a type that its signature or body depends on is being removed.", Method), "TypeDependency");
            Method.DeclaringType.Methods.Remove(Method);
        }
    }


    class EventDependency : TypeDependency {
        EventDefinition _eventDef;

        public EventDependency(EventDefinition eventDef) {
            _eventDef = eventDef;
        }

        public EventDefinition Event {
            get { return _eventDef; }
        }

        public override IMetadataTokenProvider DependingMember {
            get { return _eventDef; }
        }

        public override int Priority { get { return 2; } }

        public override void Break() {
            Trace.WriteLine(String.Format("Removing event {0} because a type that it depends on is being removed.", Event), "TypeDependency");
            Event.DeclaringType.Events.Remove(Event);
        }
    }

    class PropertyDependency : TypeDependency {
        PropertyDefinition _propertyDef;

        public PropertyDependency(PropertyDefinition propertyDef) {
            _propertyDef = propertyDef;
        }

        public PropertyDefinition Property {
            get { return _propertyDef; }
        }

        public override IMetadataTokenProvider DependingMember {
            get { return _propertyDef; }
        }

        public override int Priority { get { return 2; } }

        public override void Break() {
            Trace.WriteLine(String.Format("Removing property {0} because a type that it depends on is being removed.", Property), "TypeDependency");
            Property.DeclaringType.Properties.Remove(Property);
        }
    }

    class AttributeDependency : TypeDependency, IEquatable<AttributeDependency> {
        ICustomAttributeProvider _customAtrributeProvider;
        TypeReference _attributeType;

        public AttributeDependency(ICustomAttributeProvider dependingObject, TypeReference attributeType) {
            _customAtrributeProvider = dependingObject;
            _attributeType = attributeType;
        }

        public ICustomAttributeProvider AttributedMember {
            get { return _customAtrributeProvider; }
        }

        public TypeReference AttributeType {
            get { return _attributeType; }
        }

        public override IMetadataTokenProvider DependingMember {
            get { return _customAtrributeProvider; }
        }

        public override int Priority { get { return 1; } }

        public override void Break() {
            Trace.WriteLine(String.Format("Removing attribute {0} from {1} because the attribute is being removed.", AttributeType, AttributedMember), "TypeDependency");
            AttributedMember.CustomAttributes.RemoveWhere(ca => CecilEqualityComparer.AreEqual(ca.AttributeType, AttributeType));
        }

        public bool Equals(AttributeDependency other) {
            return (object)other != null && CecilEqualityComparer.AreEqual(other.DependingMember, this.DependingMember);
        }

        public override bool Equals(object obj) {
            var attributeDependency = obj as AttributeDependency;
            return attributeDependency != null && this.Equals(attributeDependency);
        }

        public override int GetHashCode() {
            return CecilEqualityComparer.GetHashCode(DependingMember) ^ CecilEqualityComparer.GetHashCode(AttributeType);
        }
    }

    class ExportedTypeDependency : TypeDependency {
        ExportedType _exportedType;
        ModuleDefinition _moduleDef;

        public ExportedTypeDependency(ExportedType exportedType, ModuleDefinition moduleDef) {
            _exportedType = exportedType;
            _moduleDef = moduleDef;
        }

        public ExportedType ExportedType {
            get { return _exportedType; }
        }

        public ModuleDefinition Module {
            get { return _moduleDef; }
        }

        public override IMetadataTokenProvider DependingMember {
            get { return _exportedType; }
        }

        public override int Priority { get { return 1; } }

        public override void Break() {
            Trace.WriteLine(String.Format("Removing export of type {0} because that type is being removed.", ExportedType), "TypeDependency");
            Module.ExportedTypes.Remove(ExportedType);

            TypeReferenceAndDependencies xxx = default(KeyValuePair<TypeReference, HashSet<TypeDependency>>);
        }
    }

    public class TypeDefinitionAndDependencies {
        public TypeDefinitionAndDependencies(TypeDefinition type, HashSet<TypeDependency> dependingMembers) {
            Type = type;
            DependingMembers = dependingMembers;
        }

        public HashSet<TypeDependency> DependingMembers { get; set; }

        public TypeDefinition Type { get; set; }

        public override string ToString() {
            return Type.ToString();
        }


        public static implicit operator KeyValuePair<TypeDefinition, HashSet<TypeDependency>>(TypeDefinitionAndDependencies self) {
            return new KeyValuePair<TypeDefinition, HashSet<TypeDependency>>(self.Type, self.DependingMembers);
        }

        public static implicit operator TypeDefinitionAndDependencies(KeyValuePair<TypeDefinition, HashSet<TypeDependency>> pair) {
            return new TypeDefinitionAndDependencies(pair.Key, pair.Value);
        }
    }

    public class TypeReferenceAndDependencies {
        public TypeReferenceAndDependencies(TypeReference type, HashSet<TypeDependency> dependingMembers) {
            Type = type;
            DependingMembers = dependingMembers;
        }

        public HashSet<TypeDependency> DependingMembers { get; set; }

        public TypeReference Type { get; set; }

        public override string ToString() {
            return Type.ToString();
        }

        public static implicit operator KeyValuePair<TypeReference, HashSet<TypeDependency>>(TypeReferenceAndDependencies self) {
            return new KeyValuePair<TypeReference, HashSet<TypeDependency>>(self.Type, self.DependingMembers);
        }

        public static implicit operator TypeReferenceAndDependencies(KeyValuePair<TypeReference, HashSet<TypeDependency>> pair) {
            return new TypeReferenceAndDependencies(pair.Key, pair.Value);
        }
    }
}