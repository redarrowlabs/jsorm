﻿using RedArrow.Compass.CareTeam.CaseManagement.Model.Cmmn;
using RedArrow.Titan.Sdk.Model.Data;

namespace RedArrow.Compass.CareTeam.CaseManagement.Model.TitanMappers.Cmmn
{
    public class CaseFileItemMapper : TitanData<CaseFileItem>
    {
        public CaseFileItemMapper()
        {
            OfType("case-file-item");

            WithId(x => x.Id);
            WithAttribute(x => x.Name);
            WithAttribute(x => x.State);
            WithAttribute(x => x.Multiplicity);
            WithAttribute(x => x.TitanResourceId);
            WithAttribute(x => x.TitanResourceDataType);
        }
    }
}