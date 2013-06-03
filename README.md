Piranha
=======

Piranha chews .Net libraries to make them portable.

This is still very much WIP (I wrote this tool in couple of days). The resulting assemblies can have verification errors or crash tools like Reflector.

Requirements:

 * Uses the Mono.Cecil library.

Usage:

    //piranha.exe remove-all-references          --input library.dll                        --output library.remove-all-references.dll #Useless. Crashes.
    piranha.exe remove-all-resources             --input library.dll                        --output library.remove-all-resources.dll
    piranha.exe ensure-parameterless-constructors --input library.dll                       --output library.ensure-parameterless-constructors.dll
    piranha.exe remove-method-bodies             --input library.ensure-parameterless-constructors.dll --output library.remove-method-bodies.dll
    piranha.exe remove-private-members           --input library.remove-method-bodies.dll   --output library.remove-private-members.dll
    piranha.exe remove-private-types             --input library.remove-private-members.dll --output library.remove-private-types.dll
    piranha.exe remove-code-members-types        --input library.dll                        --output library.remove-code-members-types.dll #same as remove-method-bodies + remove-private-members + remove-private-types
    piranha.exe remove-pinvoke-methods           --input library.remove-method-bodies.dll   --output library.remove-private-members.dll
    piranha.exe mark-all-references-retargetable --input library.dll                        --output library.new.dll
    piranha.exe set-target-framework             --profile ".NETPortable,Version=v4.0,Profile=Profile88"                   --input library.dll --output library.set-target-framework.dll
    piranha.exe retarget-references              --profile ".NETPortable,Version=v4.0,Profile=Profile88" [--remove-others] --input library.dll --output library.retarget-references.dll
    piranha.exe list-used-types                  --input library.dll --output library.UsedTypes.txt