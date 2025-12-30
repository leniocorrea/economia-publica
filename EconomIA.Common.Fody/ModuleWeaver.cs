using System;
using System.Collections.Generic;
using Fody;

namespace EconomIA.Common.Fody;

public class ModuleWeaver : BaseModuleWeaver {
	public override void Execute() {
	}

	public override IEnumerable<String> GetAssembliesForScanning() => [];
}
