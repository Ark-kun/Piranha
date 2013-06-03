Piranha
=======

Piranha chews .Net libraries to make them portable.

This is still very much WIP (I wrote this tool in couple of days). The resulting assemblies can have verification errors or crash tools like Reflector.

Requirements:

 * Uses the Mono.Cecil library.

Usage:

    //piranha.exe remove-all-references           -i library.dll                                   -o library.remove-all-references.dll #Useless. Crashes.
    piranha.exe remove-all-resources              -i library.dll                                   -o library.remove-all-resources.dll
    piranha.exe ensure-parameterless-constructors -i library.dll                                   -o library.ensure-parameterless-constructors.dll
    piranha.exe remove-method-bodies              -i library.ensure-parameterless-constructors.dll -o library.remove-method-bodies.dll
    piranha.exe remove-private-members --preserve-fields-of-structs -i library.remove-method-bodies.dll -o library.remove-private-members.dll
    piranha.exe remove-private-types              -i library.remove-private-members.dll            -o library.remove-private-types.dll
    piranha.exe make-skeleton --enable-breaking-verification -i library.dll                        -o library.make-skeleton.dll #same as ensure-parameterless-constructors + remove-method-bodies + remove-private-members + remove-private-types
    piranha.exe remove-pinvoke-methods            -i library.remove-method-bodies.dll              -o library.remove-private-members.dll
    piranha.exe set-target-framework --profile ".NETPortable,Version=v4.0,Profile=Profile88"                   -i library.dll -o library.set-target-framework.dll
    piranha.exe retarget-references  --profile ".NETPortable,Version=v4.0,Profile=Profile88" [--remove-others] -i library.dll -o library.retarget-references.dll
    piranha.exe mark-all-references-retargetable  -i library.dll                                   -o library.mark-all-references-retargetable.dll
    piranha.exe list-used-types                   -i library.dll -o library.UsedTypes.txt