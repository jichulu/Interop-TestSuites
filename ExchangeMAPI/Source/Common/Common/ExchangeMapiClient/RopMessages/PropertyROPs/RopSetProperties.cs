namespace Microsoft.Protocols.TestSuites.Common
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// RopSetProperties request buffer structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RopSetPropertiesRequest : ISerializable
    {
        /// <summary>
        /// Unsigned 8-bit integer. This value specifies the type of remote operation. 
        /// For this operation, this field is set to 0x0A.
        /// </summary>
        public byte RopId;

        /// <summary>
        /// Unsigned 8-bit integer. This value specifies the logon associated with this operation.
        /// </summary>
        public byte LogonId;

        /// <summary>
        /// Unsigned 8-bit integer. This index specifies the location in the Server Object Handle Table 
        /// where the handle for the input Server Object is stored.
        /// </summary>
        public byte InputHandleIndex;

        /// <summary>
        /// Unsigned 16-bit integer. This value specifies the number of bytes used for the PropertyValueCount 
        /// field and the PropertyValues field.
        /// </summary>
        public ushort PropertyValueSize;

        /// <summary>
        /// Unsigned 16-bit integer. This value specifies the number of PropertyValue structures listed in the PropertyValues field.
        /// </summary>
        public ushort PropertyValueCount;

        /// <summary>
        /// Array of TaggedPropertyValue structures. The number of structures contained in this field is 
        /// specified by the PropertyValueCount field. The size of this field, in bytes, is equal to PropertyValueSize - 2. 
        /// The format of the TaggedPropertyValue structure is specified in [MS-OXCDATA] section 2.12.4. 
        /// This field specifies the property values to be set on the object.
        /// </summary>
        public TaggedPropertyValue[] PropertyValues;

        /// <summary>
        /// Serialize the ROP request buffer.
        /// </summary>
        /// <returns>The ROP request buffer serialized.</returns>
        public byte[] Serialize()
        {
            int index = 0;
            byte[] serializeBuffer = new byte[this.Size()];

            serializeBuffer[index++] = this.RopId;
            serializeBuffer[index++] = this.LogonId;
            serializeBuffer[index++] = this.InputHandleIndex;

            Array.Copy(BitConverter.GetBytes((ushort)this.PropertyValueSize), 0, serializeBuffer, index, sizeof(ushort));
            index += sizeof(ushort);
            Array.Copy(BitConverter.GetBytes((ushort)this.PropertyValueCount), 0, serializeBuffer, index, sizeof(ushort));
            index += sizeof(ushort);

            for (int i = 0; i < this.PropertyValueCount; i++)
            {
                Array.Copy(this.PropertyValues[i].Serialize(), 0, serializeBuffer, index, this.PropertyValues[i].Size());
                index += this.PropertyValues[i].Size();
            }

            return serializeBuffer;
        }

        /// <summary>
        /// Return the size of this structure.
        /// </summary>
        /// <returns>The size of this structure.</returns>
        public int Size()
        {
            // 7 indicates sizeof(byte) * 3 + sizeof(UInt6) * 2
            int size = sizeof(byte) * 7;
            if (this.PropertyValueCount > 0)
            {
                for (int i = 0; i < this.PropertyValueCount; i++)
                {
                    size += this.PropertyValues[i].Size();
                }
            }

            return size;
        }
    }

    /// <summary>
    /// A PropertyProblem describes an error relating to an operation involving a property.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PropertyProblem : IDeserializable
    {
        /// <summary>
        /// Unsigned 16-bit integer. This value specifies an index into an array of property tags.
        /// </summary>
        public ushort Index;

        /// <summary>
        /// PropertyTag structure. This value specifies the property for which there was an error.
        /// </summary>
        public PropertyTag PropertyTag;

        /// <summary>
        /// Unsigned 32-bit integer. This value specifies the error that occurred when processing this property.
        /// </summary>
        public uint ErrorCode;

        /// <summary>
        /// Deserialize the ROP response buffer.
        /// </summary>
        /// <param name="ropBytes">ROPs bytes in response.</param>
        /// <param name="startIndex">The start index of this ROP.</param>
        /// <returns>The size of response buffer structure.</returns>
        public int Deserialize(byte[] ropBytes, int startIndex)
        {
            int index = startIndex;
            this.Index = (ushort)BitConverter.ToInt16(ropBytes, index);
            index += sizeof(ushort);
            index += this.PropertyTag.Deserialize(ropBytes, index);
            this.ErrorCode = (uint)BitConverter.ToInt32(ropBytes, index);
            index += sizeof(uint);
            return index - startIndex;
        }
    }

    /// <summary>
    /// RopSetProperties response buffer structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RopSetPropertiesResponse : IDeserializable
    {
        /// <summary>
        /// Unsigned 8-bit integer. This value specifies the type of remote operation. 
        /// For this operation, this field is set to 0x0A.
        /// </summary>
        public byte RopId;

        /// <summary>
        /// Unsigned 8-bit integer. This index MUST be set to the InputHandleIndex specified in the request.
        /// </summary>
        public byte InputHandleIndex;

        /// <summary>
        /// Unsigned 32-bit integer. This value specifies the status of the remote operation. 
        /// For this response, this field is set to 0x00000000.
        /// </summary>
        public uint ReturnValue;

        /// <summary>
        /// Unsigned 16-bit integer. This value specifies the number of PropertyProblem structures in the PropertyProblems field.
        /// </summary>
        public ushort PropertyProblemCount;

        /// <summary>
        /// Array of PropertyProblem structures. The number of structures contained in this field is 
        /// specified by the PropertyProblemCount field.
        /// </summary>
        public PropertyProblem[] PropertyProblems;

        /// <summary>
        /// Deserialize the ROP response buffer.
        /// </summary>
        /// <param name="ropBytes">ROPs bytes in response.</param>
        /// <param name="startIndex">The start index of this ROP.</param>
        /// <returns>The size of response buffer structure.</returns>
        public int Deserialize(byte[] ropBytes, int startIndex)
        {
            int index = startIndex;
            this.RopId = ropBytes[index++];
            this.InputHandleIndex = ropBytes[index++];
            this.ReturnValue = (uint)BitConverter.ToInt32(ropBytes, index);
            index += sizeof(uint);

            // Only success response has below fields
            if (this.ReturnValue == 0)
            {
                this.PropertyProblemCount = (ushort)BitConverter.ToInt16(ropBytes, index);
                index += sizeof(ushort);
                if (this.PropertyProblemCount >= 0)
                {
                    this.PropertyProblems = new PropertyProblem[this.PropertyProblemCount];
                    for (int i = 0; i < this.PropertyProblemCount; i++)
                    {
                        index += this.PropertyProblems[i].Deserialize(ropBytes, index);
                    }
                }
            }

            return index - startIndex;
        }
    }
}