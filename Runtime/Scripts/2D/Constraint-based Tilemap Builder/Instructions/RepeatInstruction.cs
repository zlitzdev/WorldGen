using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using Rng = System.Random;

namespace Zlitz.Extra2D.WorldGen
{
    internal sealed class RepeatInstruction : TilemapModulePlaceInstruction.Instruction
    {
        private TilemapModulePlaceInstruction.Instruction m_instruction;

        private int m_minCount;
        private int m_maxCount;

        private int[] m_counts;
        private int m_countIndex;

        private TilemapModulePlaceInstruction.Instruction[] m_instructions;

        public RepeatInstruction(TilemapModulePlaceInstruction.Instruction instruction, int minCount, int maxCount)
        {
            m_instruction = instruction;
            m_minCount = Mathf.Max(0, Mathf.Min(minCount, maxCount));
            m_maxCount = Mathf.Max(0, Mathf.Max(minCount, maxCount));
        }

        public override string debugName => $"Repeat({m_instruction.debugName}, {m_minCount}, {m_maxCount})";

        public override void Reset(TilemapModuleSet moduleSet, TilemapModulePlaceInstruction.LayoutStateStack states, Rng rng)
        {
            List<int> counts = new List<int>();
            for (int i = m_minCount; i<= m_maxCount ; i++)
            {
                counts.Add(i);
            }
            m_counts = counts.OrderBy(i => rng.Next()).ToArray();
            m_countIndex = 0;

            if (m_countIndex < m_counts.Length)
            {
                m_instructions = new TilemapModulePlaceInstruction.Instruction[m_counts[m_countIndex]];
                for (int i = 0; i < m_instructions.Length; i++)
                {
                    m_instructions[i] = Copy(m_instruction);
                    m_instructions[i].repeatIndex = i;
                }
            }
        }

        public override bool Execute(TilemapModuleSet moduleSet, TilemapModulePlaceInstruction.LayoutStateStack states, Rng rng)
        {
            return ExecuteRecursive(moduleSet, m_instructions, 0, states, rng);
        }

        public override bool Next(TilemapModuleSet moduleSet, Rng rng)
        {
            m_countIndex++;
            if (m_countIndex < m_counts.Length)
            {
                m_instructions = new TilemapModulePlaceInstruction.Instruction[m_counts[m_countIndex]];
                for (int i = 0; i < m_instructions.Length; i++)
                {
                    m_instructions[i] = Copy(m_instruction);
                }
            }

            return false;
        }

        protected override TilemapModulePlaceInstruction.Instruction Copy()
        {
            return new RepeatInstruction(m_instruction, m_minCount, m_maxCount);
        }

        private bool ExecuteRecursive(TilemapModuleSet moduleSet, TilemapModulePlaceInstruction.Instruction[] instructions, int index, TilemapModulePlaceInstruction.LayoutStateStack states, Rng rng)
        {
            if (index >= instructions.Length)
            {
                return true;
            }

            TilemapModulePlaceInstruction.Instruction instruction = instructions[index];
            instruction.Reset(moduleSet, states, rng);

            while (true)
            {
                states.Push($"{debugName} - Instruction {index}: {instruction.debugName}");

                bool execute = instruction.Execute(moduleSet, states, rng);
                if (execute && ExecuteRecursive(moduleSet, instructions, index + 1, states, rng))
                {
                    return true;
                }

                states.Pop();
                if (instruction.Next(moduleSet, rng))
                {
                    continue;
                }
                return false;
            }
        }

    }
}
