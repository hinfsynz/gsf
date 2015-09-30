'*******************************************************************************************************
'  FrequencyDefinition.vb - IEEE C37.118 Frequency definition
'  Copyright � 2005 - TVA, all rights reserved - Gbtc
'
'  Build Environment: VB.NET, Visual Studio 2003
'  Primary Developer: James R Carroll, System Analyst [TVA]
'      Office: COO - TRNS/PWR ELEC SYS O, CHATTANOOGA, TN - MR 2W-C
'       Phone: 423/751-2827
'       Email: jrcarrol@tva.gov
'
'  Code Modification History:
'  -----------------------------------------------------------------------------------------------------
'  11/12/2004 - James R Carroll
'       Initial version of source generated
'
'*******************************************************************************************************

Imports System.Text
Imports TVA.Interop
Imports TVA.Shared.Bit

Namespace EE.Phasor.IEEEC37_118

    Public Class FrequencyDefinition

        Inherits FrequencyDefinitionBase

        Public Sub New(ByVal parent As ConfigurationCell)

            MyBase.New(parent)

        End Sub

        Public Sub New(ByVal parent As ConfigurationCell, ByVal binaryImage As Byte(), ByVal startIndex As Integer)

            MyBase.New(parent)

            Dim nominalFrequencyFlags As Int16 = EndianOrder.BigEndian.ToInt16(binaryImage, startIndex)

            ' TODO: Move nominal frequency into config cell definition
            parent.Parent.NominalFrequency = IIf(nominalFrequencyFlags And Bit0 > 0, LineFrequency.Hz50, LineFrequency.Hz60)

        End Sub

        Public Sub New(ByVal frequencyDefinition As IFrequencyDefinition)

            MyBase.New(frequencyDefinition)

        End Sub

        Public Overrides ReadOnly Property InheritedType() As System.Type
            Get
                Return Me.GetType
            End Get
        End Property

    End Class

End Namespace