using Rng = System.Random;

namespace Zlitz.Extra2D.WorldGen
{
    internal sealed class OptionalInstruction : TilemapModulePlaceInstruction.Instruction
    {
        private TilemapModulePlaceInstruction.Instruction m_instruction;

        public OptionalInstruction(TilemapModulePlaceInstruction.Instruction instruction)
        {
            m_instruction = Copy(instruction);
        }

        public override string debugName => $"Optional({m_instruction.debugName})";

        public override void Reset(TilemapModuleSet moduleSet, TilemapModulePlaceInstruction.LayoutStateStack states, Rng rng)
        {
            m_instruction.Reset(moduleSet, states, rng);
        }

        public override bool Execute(TilemapModuleSet moduleSet, TilemapModulePlaceInstruction.LayoutStateStack states, Rng rng)
        {
            while (true)
            {
                states.Push($"{debugName} - {m_instruction.debugName}");
                bool execute = m_instruction.Execute(moduleSet, states, rng);
                if (execute)
                {
                    states.PopUnder();
                    return true;
                }

                states.Pop();
                if (!m_instruction.Next(moduleSet, rng))
                {
                    break;
                }
            }

            return true;
        }

        public override bool Next(TilemapModuleSet moduleSet, Rng rng)
        {
            return false;
        }

        protected override TilemapModulePlaceInstruction.Instruction Copy()
        {
            return new OptionalInstruction(m_instruction);
        }
    }
}
