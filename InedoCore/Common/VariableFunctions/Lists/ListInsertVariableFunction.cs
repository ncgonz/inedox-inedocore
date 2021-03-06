﻿using Inedo.Documentation;
using Inedo.ExecutionEngine;
#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.VariableFunctions;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.VariableFunctions;
#endif
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Collections;

namespace Inedo.Extensions.VariableFunctions.Lists
{
    [ScriptAlias("ListInsert")]
    [Description("Inserts an item into a list.")]
    [Tag("lists")]
    public sealed class ListInsertVariableFunction : CommonVectorVariableFunction
    {
        [VariableFunctionParameter(0)]
        [DisplayName("list")]
        [Description("The list.")]
        public IEnumerable<RuntimeValue> List { get; set; }

        [VariableFunctionParameter(1)]
        [DisplayName("item")]
        [Description("The item.")]
        public RuntimeValue Item { get; set; }

        [VariableFunctionParameter(2, Optional = true)]
        [DisplayName("index")]
        [Description("The index.")]
        public int? Index { get; set; }

        protected override IEnumerable EvaluateVector(object context)
        {
            var list = this.List.ToList();
            if (this.Index.HasValue)
            {
                list.Insert(this.Index.Value, this.Item);
            }
            else
            {
                list.Add(this.Item);
            }
            return list;
        }
    }
}
