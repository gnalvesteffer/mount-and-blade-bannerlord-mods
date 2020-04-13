using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace Banks
{
    internal class BankDataSaveableTypeDefiner : SaveableTypeDefiner
    {
        public BankDataSaveableTypeDefiner() : base(42069247)
        {
        }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(BankData), 1);
            AddClassDefinition(typeof (LoanQuest), 2);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(Dictionary<string, BankData>));
        }
    }
}
