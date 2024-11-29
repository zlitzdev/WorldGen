using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Rng = System.Random;

namespace Zlitz.Extra2D.WorldGen
{
    public class RandomInstruction : TilemapModulePlaceInstruction.Instruction
    {
        private TilemapModulePlaceInstruction.Instruction[] m_instructions;

        public RandomInstruction(TilemapModulePlaceInstruction.Instruction[] instructions)
        {
            m_instructions = instructions.Select(i => Copy(i)).Where(i => i != null).ToArray();
        }

        public override string debugName => $"Random({string.Join(", ", m_instructions.Select(i => i.debugName))})";

        public override void Reset(TilemapModuleSet moduleSet, TilemapModulePlaceInstruction.LayoutStateStack states, Rng rng)
        {
            foreach (TilemapModulePlaceInstruction.Instruction instruction in m_instructions)
            {
                instruction.Reset(moduleSet, states, rng);
            }
        }

        public override bool Execute(TilemapModuleSet moduleSet, TilemapModulePlaceInstruction.LayoutStateStack states, Rng rng)
        {
            return ExecuteRecursive(moduleSet, m_instructions, 0, states, rng);
        }

        public override bool Next(TilemapModuleSet moduleSet, Rng rng)
        {
            return false;
        }

        protected override TilemapModulePlaceInstruction.Instruction Copy()
        {
            return new RandomInstruction(m_instructions);
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
