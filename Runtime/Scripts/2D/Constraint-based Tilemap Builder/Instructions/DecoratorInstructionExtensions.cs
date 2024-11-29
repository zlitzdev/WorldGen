namespace Zlitz.Extra2D.WorldGen
{
    public static class DecoratorInstructionExtensions
    {
        public static TilemapModulePlaceInstruction.Instruction Optional(this TilemapModulePlaceInstruction.Instruction instruction)
        {
            return new OptionalInstruction(instruction);
        }

        public static TilemapModulePlaceInstruction.Instruction Repeat(this TilemapModulePlaceInstruction.Instruction instruction, int minCount, int maxCount)
        {
            return new RepeatInstruction(instruction, minCount, maxCount);
        }
    }
}
