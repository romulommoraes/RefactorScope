using System;

namespace RefactorScope.Core.Model
{
    /// <summary>
    /// Representa uma suspeita de acoplamento arquitetural implícito.
    /// 
    /// Detectada por heurísticas como:
    /// - fan-out elevado
    /// - fan-in baixo
    /// - dominância de dependência
    /// </summary>
    public sealed class CouplingSuspicion
    {
        public string TypeName { get; }

        public string Module { get; }

        public string TargetModule { get; }

        public int FanOut { get; }

        public int FanIn { get; }

        public double Dominance { get; }

        public int Volume { get; }

        public CouplingSuspicion(
            string typeName,
            string module,
            string targetModule,
            int fanOut,
            int fanIn,
            double dominance,
            int volume)
        {
            TypeName = typeName;
            Module = module;
            TargetModule = targetModule;
            FanOut = fanOut;
            FanIn = fanIn;
            Dominance = dominance;
            Volume = volume;
        }
    }
}