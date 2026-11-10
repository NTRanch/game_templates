// Copyright Nice Thoughts Ranch. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using UnrealBuildTool;

public class GameShell : ModuleRules
{
	public GameShell(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;

		PublicDependencyModuleNames.AddRange(new string[]
		{
			"Core",
			"CoreUObject",
			"Engine",
			"InputCore",
			"UMG",
			"Slate",
			"SlateCore",
			// Json: the save envelope's PayloadJson is produced and
			// consumed with the engine's Json module by the game layer.
			"Json"
		});

		PrivateDependencyModuleNames.AddRange(new string[]
		{
		});
	}
}
