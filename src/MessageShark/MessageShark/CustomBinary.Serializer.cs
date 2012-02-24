﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;

namespace MessageShark {
    public static partial class CustomBinary {
        public static byte[] DateTimeToByteArray(DateTime date) {
            return BitConverter.GetBytes(date.Ticks);
        }

        public static byte[] DecimalToByteArray(decimal value) {
            using (var ms = new MemoryStream()) {
                using (var bw = new BinaryWriter(ms)) {
                    bw.Write(value);
                    return ms.ToArray();
                }
            }
        }

        public static byte[] StringToByteArray(string value) {
            var size = UTF8.GetByteCount(value);
            var buffer = new byte[size];
            UTF8.GetBytes(value, 0, size, buffer, 0);
            return buffer;
        }

        public static byte[] Int16ToBytes(short value) {
            if (value <= 255) return new byte[] { (byte)value };
            byte[] buffer = new byte[2];
            var index = 0;
            var sValue = 0;
            unchecked {
                buffer[index++] = (byte)value;
                if ((sValue = (value >> 8)) > 0) {
                    buffer[index++] = (byte)sValue;
                }
            }
            var buffer2 = new byte[index];
            Buffer.BlockCopy(buffer, 0, buffer2, 0, index);
            return buffer2;
        }

        public static byte[] Int32ToBytes(int value) {
            if (value <= 255) return new byte[] { (byte)value };
            byte[] buffer = new byte[4];
            var index = 0;
            var sValue = 0;
            unchecked {
                buffer[index++] = (byte)value;
                if ((sValue = (value >> 8)) > 0) {
                    buffer[index++] = (byte)sValue;
                    if ((sValue = (value >> 16)) > 0) {
                        buffer[index++] = (byte)sValue;
                        if ((sValue = (value >> 24)) > 0) buffer[index++] = (byte)sValue;
                    }
                }
            }
            var buffer2 = new byte[index];
            Buffer.BlockCopy(buffer, 0, buffer2, 0, index);
            return buffer2;
        }

        public static byte[] Int64ToBytes(long value) {
            if (value <= 255) return new byte[] { (byte)value };
            const int msb = 0xff;
            byte[] buffer = new byte[8];
            var index = 0;
            var sValue = 0L;
            unchecked {
                buffer[index++] = (byte)(value & msb);
                if ((sValue = (value >> 8) & msb) > 0) {
                    buffer[index++] = (byte)sValue;
                    if ((sValue = (value >> 16) & msb) > 0) {
                        buffer[index++] = (byte)sValue;
                        if ((sValue = (value >> 24) & msb) > 0) {
                            buffer[index++] = (byte)sValue;
                            if ((sValue = (value >> 32) & msb) > 0) {
                                buffer[index++] = (byte)sValue;
                                if ((sValue = (value >> 40) & msb) > 0) {
                                    buffer[index++] = (byte)sValue;
                                    if ((sValue = (value >> 48) & msb) > 0) {
                                        buffer[index++] = (byte)sValue;
                                        if ((sValue = (value >> 56) & msb) > 0) 
                                            buffer[index++] = (byte)sValue;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            var buffer2 = new byte[index];
            Buffer.BlockCopy(buffer, 0, buffer2, 0, index);
            return buffer2;
        }

        static void WriteUnBufferedBytes(CustomBuffer customBuffer, byte[] buffer, int tag) {
            customBuffer.Write(EncodeLength(buffer.Length, tag));
            customBuffer.Write(buffer);            
        }

        static void WriteBufferedBytes(CustomBuffer customBuffer, byte[] buffer, int tag) {
            var lengthBuffer = Int32ToBytes(buffer.Length);
            customBuffer.Write(EncodeLength(lengthBuffer.Length, tag));
            customBuffer.Write(lengthBuffer);
            customBuffer.Write(buffer);
        }

        public static void WriteByteToBuffer(CustomBuffer customBuffer, byte value, int tag, bool isTargetCollection) {
            if (value == 0 && !isTargetCollection) return;
            WriteUnBufferedBytes(customBuffer, new [] { value }, tag);
        }

        public static void WriteEnumToBuffer(CustomBuffer customBuffer, Enum value, int tag, bool isTargetCollection) {
            var enumValue = Convert.ToInt32(value);
            if (enumValue == 0 && !isTargetCollection) return;
            WriteUnBufferedBytes(customBuffer, Int32ToBytes(enumValue), tag);
        }

        public static void WriteNullableTimeSpanToBuffer(CustomBuffer customBuffer, TimeSpan? value, int tag, bool isTargetCollection) {
            WriteTimeSpanToBuffer(customBuffer, value ?? TimeSpan.Zero, tag, isTargetCollection);
        }

        public static void WriteTimeSpanToBuffer(CustomBuffer customBuffer, TimeSpan value, int tag, bool isTargetCollection) {
            if (value == TimeSpan.Zero && !isTargetCollection) return;
            byte[] buffer = null;
            if (value == TimeSpan.MaxValue) buffer = MaxTimeSpanBytes;
            else if (value == TimeSpan.MinValue) buffer = MinTimeSpanBytes;
            else {
                int tickIndex = -1;
                long ticksValue;
                var ticks = value.Ticks;
                for (var i = 0; i < TimeSpanTicksLength; i++) {
                    if (ticks % (ticksValue = TimeSpanTicks[i]) == 0) {
                        ticks /= ticksValue;
                        tickIndex = i;
                        break;
                    }
                }
                if (ticks <= 255 && tickIndex != -1) buffer = new byte[] { (byte)tickIndex, (byte)ticks };
                else {
                    var ticksBuffer = Int64ToBytes(ticks);
                    buffer = new byte[ticksBuffer.Length + 1];
                    buffer[0] = (byte)(tickIndex > - 1 ? tickIndex : TimeSpanTicksLength);
                    Buffer.BlockCopy(ticksBuffer, 0, buffer, 1, ticksBuffer.Length);
                }
            }
            WriteUnBufferedBytes(customBuffer, buffer, tag);
        }

        public static void WriteUInt64ToBuffer(CustomBuffer customBuffer, ulong value, int tag, bool isTargetCollection) {
            if (value == 0 && !isTargetCollection) return;
            WriteUnBufferedBytes(customBuffer, Int64ToBytes((long)value), tag);
        }

        public static void WriteUInt32ToBuffer(CustomBuffer customBuffer, uint value, int tag, bool isTargetCollection) {
            if (value == 0 && !isTargetCollection) return;
            WriteUnBufferedBytes(customBuffer, Int32ToBytes((int)value), tag);
        }

        public static void WriteUInt16ToBuffer(CustomBuffer customBuffer, ushort value, int tag, bool isTargetCollection) {
            if (value == 0 && !isTargetCollection) return;
            WriteUnBufferedBytes(customBuffer, Int16ToBytes((short)value), tag);
        }

        public static void WriteFloatToBuffer(CustomBuffer customBuffer, float value, int tag, bool isTargetCollection) {
            if (value == 0f && !isTargetCollection) return;
            unsafe {
                WriteUnBufferedBytes(customBuffer, Int32ToBytes(*((int*)&value)), tag);
            }
        }

        public static void WriteDecimalToBuffer(CustomBuffer customBuffer, decimal value, int tag, bool isTargetCollection) {
            if (value == 0m && !isTargetCollection) return;
            WriteBufferedBytes(customBuffer, DecimalToByteArray(value), tag);
        }

        public static void WriteInt64ToBuffer(CustomBuffer customBuffer, long value, int tag, bool isTargetCollection) {
            if (value == 0 && !isTargetCollection) return;
            WriteUnBufferedBytes(customBuffer, Int64ToBytes(value), tag);
        }

        public static void WriteInt16ToBuffer(CustomBuffer customBuffer, short value, int tag, bool isTargetCollection) {
            if (value == 0 && !isTargetCollection) return;
            WriteUnBufferedBytes(customBuffer, Int16ToBytes(value), tag);
        }

        public static void WriteDoubleToBuffer(CustomBuffer customBuffer, double value, int tag, bool isTargetCollection) {
            if (value == 0d && !isTargetCollection) return;
            unsafe {
                WriteUnBufferedBytes(customBuffer, Int64ToBytes(*((long*)&value)), tag);
            }
        }

        public static void WriteCharToBuffer(CustomBuffer customBuffer, char value, int tag, bool isTargetCollection) {
            if (value == char.MinValue && !isTargetCollection) return;
            WriteUnBufferedBytes(customBuffer, Int16ToBytes((short)value), tag);  
        }

        public static void WriteBoolToBuffer(CustomBuffer customBuffer, bool value, int tag, bool isTargetCollection) {
            if (value == false && !isTargetCollection) return;
            WriteUnBufferedBytes(customBuffer, BooleanBytes, tag);
        }

        public static void WriteInt32ToBuffer(CustomBuffer customBuffer, int value, int tag, bool isTargetCollection) {
            if (value == 0 && !isTargetCollection) return;
            WriteUnBufferedBytes(customBuffer, Int32ToBytes(value), tag);
        }

        public static void WriteDateTimeToBuffer(CustomBuffer customBuffer, DateTime value, int tag, bool isTargetCollection) {
            if (value == DateTime.MinValue && !isTargetCollection) return;
            WriteUnBufferedBytes(customBuffer, DateTimeToByteArray(value), tag);
        }

        public static void WriteStringToBuffer(CustomBuffer customBuffer, string value, int tag, bool isTargetCollection) {
            if (value == null && !isTargetCollection) return;
            WriteBufferedBytes(customBuffer, StringToByteArray(value), tag);
        }

        public static void WriteGuidToBuffer(CustomBuffer customBuffer, Guid value, int tag, bool isTargetCollection) {
            if (value == Guid.Empty && !isTargetCollection) return;
            WriteBufferedBytes(customBuffer, value.ToByteArray(), tag);
        }

        public static void WriteTypeIDFor(CustomBuffer customBuffer, Type baseType, Type type) {
            if (baseType == ObjectType) return;
            TypeIDByteArray[0] = TypeMapping[baseType][type];
            customBuffer.Write(TypeIDByteArray);
        }

        public static void WriteCollectionHeader(CustomBuffer customBuffer, ICollection collection, int tag) {
            WriteUnBufferedBytes(customBuffer, Int32ToBytes(collection.Count), tag);
        }
    }
}
