Piranha
=======

Piranha chews .Net libraries to make them portable.

This is still very much WIP (I wrote this tool in couple of days). The resulting assemblies can have verification errors or crash tools like Reflector.

Requirements:

 * Uses the Mono.Cecil library.

Usage:

    piranha.exe remove-all-references --input library.dll --output library.new.dll
    piranha.exe remove-all-resources --input library.dll --output library.new.dll
    piranha.exe remove-method-bodies --input library.dll --output library.new.dll
    piranha.exe remove-private-members --input library.new.dll --output library.new2.dll *needs fixing*
    piranha.exe remove-private-types --input library.new2.dll --output library.new3.dll
    piranha.exe mark-all-references-retargetable -i library.dll -o library.new.dll
    piranha.exe set-target-framework --profile ".NETPortable,Version=v4.0,Profile=Profile88" -i library.dll -o library.new.dll
    piranha.exe retarget-references --profile ".NETPortable,Version=v4.0,Profile=Profile88" [--remove-others] -i library.dll -o library.new.dll
    piranha.exe list-used-types -i library.dll -o library.UsedTypes.txt