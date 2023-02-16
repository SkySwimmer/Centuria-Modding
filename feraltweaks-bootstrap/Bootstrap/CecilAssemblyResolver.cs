using System.IO;
using System.Collections.Generic;
using Mono.Cecil;
using System.Reflection;

namespace FeralTweaksBootstrap
{
    internal class AltDirCecilAssemblyResolver : DefaultAssemblyResolver
    {
        private string path;
        private Dictionary<string, AssemblyDefinition> cecilDefs = new Dictionary<string, AssemblyDefinition>();

        public AltDirCecilAssemblyResolver(string path)
        {
            this.path = path;
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            return Resolve(name, new ReaderParameters(ReadingMode.Immediate));
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            AssemblyDefinition def = Resolve(name.Name, parameters);
            if (def == null)
                return base.Resolve(name, parameters);
            return def;
        }

        private AssemblyDefinition Resolve(string name, ReaderParameters parameters)
        {
            if (cecilDefs.ContainsKey(name))
                return cecilDefs[name];

            // Find assembly
            if (File.Exists(path + "/" + name + ".dll"))
            {
                AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(path + "/" + name + ".dll", parameters);
                cecilDefs[asm.Name.Name] = asm;
                return asm;
            }

            // Find system assembly
            if (File.Exists("CoreCLR/" + name + ".dll"))
            {
                AssemblyDefinition asm = AssemblyDefinition.ReadAssembly("CoreCLR/" + name + ".dll", parameters);
                cecilDefs[asm.Name.Name] = asm;
                return asm;
            }

            return null;
        }

        internal List<AssemblyDefinition> ToList()
        {
            List<AssemblyDefinition> asms = new List<AssemblyDefinition>();
            foreach (FileInfo file in new DirectoryInfo(path).GetFiles("*.dll"))
            {
                asms.Add(Resolve(file.Name.Remove(file.Name.LastIndexOf(".dll")), new ReaderParameters(ReadingMode.Deferred) { AssemblyResolver = this }));
            }
            return asms;
        }
    }

}