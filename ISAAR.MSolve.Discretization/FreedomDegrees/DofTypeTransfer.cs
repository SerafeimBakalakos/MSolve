using ISAAR.MSolve.Discretization.FreedomDegrees;
using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.Discretization.FreedomDegrees
{
    public enum DofTypeDto
    {
        TranslationX, TranslationY, TranslationZ, RotationX, RotationY, RotationZ, Temperature, PorePressure
    }

    public static class DofTypeTransfer
    {
        public static Dictionary<IDofType, DofTypeDto> Serialization { get; }
        public static Dictionary<DofTypeDto, IDofType> Deserialization { get; }

        static DofTypeTransfer()
        {
            Serialization = new Dictionary<IDofType, DofTypeDto>();
            Serialization[StructuralDof.TranslationX] = DofTypeDto.TranslationX;
            Serialization[StructuralDof.TranslationY] = DofTypeDto.TranslationY;
            Serialization[StructuralDof.TranslationZ] = DofTypeDto.TranslationZ;
            Serialization[StructuralDof.RotationX] = DofTypeDto.RotationX;
            Serialization[StructuralDof.RotationY] = DofTypeDto.RotationY;
            Serialization[StructuralDof.RotationZ] = DofTypeDto.RotationZ;
            Serialization[ThermalDof.Temperature] = DofTypeDto.Temperature;
            Serialization[PorousMediaDof.Pressure] = DofTypeDto.PorePressure;

            Deserialization = new Dictionary<DofTypeDto, IDofType>();
            Deserialization[DofTypeDto.TranslationX] = StructuralDof.TranslationX;
            Deserialization[DofTypeDto.TranslationY] = StructuralDof.TranslationY;
            Deserialization[DofTypeDto.TranslationZ] = StructuralDof.TranslationZ;
            Deserialization[DofTypeDto.RotationX] = StructuralDof.RotationX;
            Deserialization[DofTypeDto.RotationY] = StructuralDof.RotationY;
            Deserialization[DofTypeDto.RotationZ] = StructuralDof.RotationZ;
            Deserialization[DofTypeDto.Temperature] = ThermalDof.Temperature;
            Deserialization[DofTypeDto.PorePressure] = PorousMediaDof.Pressure;
        }
    }
}
