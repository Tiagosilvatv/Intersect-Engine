﻿using System;
using System.ComponentModel.DataAnnotations.Schema;

using Intersect.Enums;
using Intersect.GameObjects.Switches_and_Variables;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.GameObjects
{
    public class ServerVariableBase : DatabaseObject<ServerVariableBase>
    {
        //Identifier used for event chat variables to display the value of this variable/switch.
        //See https://www.ascensiongamedev.com/topic/749-event-text-variables/ for usage info.
        public string TextId { get; set; }
        public VariableDataTypes Type { get; set; } = VariableDataTypes.Boolean;

        [NotMapped]
        public VariableValue Value { get; set; } = new VariableValue();

        [Column("Value")]
        [JsonIgnore]
        public string DBValue
        {
            get => Value.JsonValue;
            private set => Value.JsonValue = value;
        }

        [JsonConstructor]
        public ServerVariableBase(Guid id) : base(id)
        {
            Name = "New Global Variable";
        }

        public ServerVariableBase()
        {
            Name = "New Global Variable";
        }
    }
}