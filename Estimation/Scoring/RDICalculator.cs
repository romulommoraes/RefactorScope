using RefactorScope.Core.Context;
using RefactorScope.Core.Metrics;
using RefactorScope.Estimation.Classification;
using RefactorScope.Estimation.Models;

namespace RefactorScope.Estimation.Scoring
{
	/// <summary>
	/// Calcula o Refactor Difficulty Index (RDI).
	///
	/// O RDI é um indicador heurístico de dificuldade de refatoração
	/// que agrega múltiplos sinais estruturais do sistema em um único valor.
	///
	/// O índice varia de 0 a 100 e é composto por quatro dimensões
	/// normalizadas (0–25 cada):
	///
	/// • StructuralRisk
	///     Mede degradação estrutural da arquitetura
	///     (drift de namespace + candidatos unresolved).
	///
	/// • CouplingPressure
	///     Mede pressão arquitetural causada por dependências
	///     excessivas entre módulos.
	///
	/// • SizePressure
	///     Mede pressão estrutural causada pela densidade
	///     de classes no sistema.
	///
	/// • RefactorActions
	///     Heurística baseada em sinais de refactor detectados
	///     diretamente no modelo estrutural.
	///
	/// Importante:
	/// Este cálculo depende apenas de:
	///
	/// • AnalysisContext (modelo estrutural)
	/// • ArchitecturalMetrics (métricas arquiteturais agregadas)
	///
	/// Não depende do módulo Statistics, mantendo o Estimation
	/// completamente desacoplado da camada observacional.
	/// </summary>
	public static class RDICalculator
	{
		public static RefactorDifficultyIndex Calculate(
			AnalysisContext context,
			ArchitecturalMetrics metrics)
		{
			// 1. Risco estrutural da arquitetura
			var structural =
				StructuralRiskModel.Compute(
					metrics.NamespaceDriftRatio,
					metrics.UnresolvedCandidateRatio);

			// 2. Pressão causada por acoplamento arquitetural
			var coupling =
				CouplingPressureModel.Compute(
					metrics.MeanCoupling);

			// 3. Pressão estrutural causada pelo tamanho do sistema
			var size =
				SizePressureModel.Compute(context);

			// 4. Heurística de ações de refactor detectadas
			var actions =
				RefactorClassifier.ComputeActionScore(context);

			return new RefactorDifficultyIndex(
				structural,
				coupling,
				size,
				actions
			);
		}
	}
}