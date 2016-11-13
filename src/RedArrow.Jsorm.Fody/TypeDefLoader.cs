﻿using System.Linq;
using Mono.Cecil;

namespace RedArrow.Jsorm
{
	public partial class ModuleWeaver
	{
		private TypeDefinition _sessionTypeDef;
		private TypeDefinition _guidTypeDef;

		private void LoadTypeDefinitions()
		{
			_sessionTypeDef = JsormAssembly.MainModule.GetType("RedArrow.Jsorm.Session.IModelSession");

			var msCoreAssemblyDef = AssemblyResolver.Resolve("mscorlib");
			_guidTypeDef = msCoreAssemblyDef.MainModule.Types.First(x => x.Name == "Guid");
		}
	}
}
