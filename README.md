Piranha
=======

Piranha chews .Net libraries to make them portable.

Usage:

	piranha.exe make-portable-skeleton --profile ".NETPortable,Version=v4.0,Profile=Profile96" -i library.dll -o library.portable-skeleton.dll

Short: This command produces the skeleton/reference library to which portable libraries can link.

Long: This command removes all resources, code, private members, private types and P/Invoke methods then retargets the assembly to the specified (preferably portable) .Net Framework profile and removes all members that reference types outside the assembly, framework assembly set or retargetable assembly references.


Advanced usage:

    //piranha.exe remove-all-references           -i library.dll                                   -o library.remove-all-references.dll #Useless. Crashes.
    piranha.exe remove-all-resources              -i library.dll                                   -o library.remove-all-resources.dll
    piranha.exe ensure-parameterless-constructors -i library.dll                                   -o library.ensure-parameterless-constructors.dll
    piranha.exe remove-method-bodies              -i library.ensure-parameterless-constructors.dll -o library.remove-method-bodies.dll
    piranha.exe remove-private-members --preserve-fields-of-structs -i library.remove-method-bodies.dll -o library.remove-private-members.dll
    piranha.exe remove-private-types              -i library.remove-private-members.dll            -o library.remove-private-types.dll
    piranha.exe make-skeleton --enable-breaking-verification -i library.dll                        -o library.make-skeleton.dll #same as remove-all-resources + ensure-parameterless-constructors + remove-method-bodies + remove-private-members + remove-private-types
    piranha.exe remove-pinvoke-methods            -i library.remove-method-bodies.dll              -o library.remove-private-members.dll
    piranha.exe set-target-framework --profile ".NETPortable,Version=v4.0,Profile=Profile88"                   -i library.dll -o library.set-target-framework.dll
    piranha.exe retarget-references  --profile ".NETPortable,Version=v4.0,Profile=Profile88" [--remove-others] -i library.dll -o library.retarget-references.dll
    piranha.exe remove-external-types-usage [--profile ".NETPortable,Version=v4.0,Profile=Profile88"] [--remove-non-retargetable] -i library.dll -o library.remove-external-types-usage.dll
    piranha.exe retarget-assembly    --profile ".NETPortable,Version=v4.0,Profile=Profile88" [--remove-others] -i library.dll -o library.retarget-assembly.dll #same as set-target-framework + retarget-references + emove-external-types-usage
    piranha.exe make-portable-skeleton --profile ".NETPortable,Version=v4.0,Profile=Profile88"                 -i library.dll -o library.make-portable-skeleton.dll #same as make-skeleton + remove-pinvoke-methods + retarget-assembly
    piranha.exe mark-all-references-retargetable  -i library.dll                                   -o library.mark-all-references-retargetable.dll
    piranha.exe list-used-types                   -i library.dll -o library.UsedTypes.txt

Requirements:

 * Uses the Mono.Cecil library.
