using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace Banks
{
    public class BankDataSaveableTypeDefiner : SaveableTypeDefiner
    {
        public BankDataSaveableTypeDefiner() : base(42069247)
        {
        }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(BankData), 1);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(Dictionary<MBGUID, BankData>));
        }
    }
}
