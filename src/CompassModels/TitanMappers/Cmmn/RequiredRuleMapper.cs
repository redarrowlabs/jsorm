﻿using RedArrow.Compass.CareTeam.CaseManagement.Model.Cmmn;
using RedArrow.Titan.Sdk.Model.Data;

namespace RedArrow.Compass.CareTeam.CaseManagement.Model.TitanMappers.Cmmn
{
    public class RequiredRuleMapper : TitanData<RequiredRule>
    {
        public RequiredRuleMapper()
        {
            OfType("case-rule");

            WithId(x => x.Id);
            WithAttribute(x => x.Name);
            WithAttribute(x => x.Type);
        }
    }
}