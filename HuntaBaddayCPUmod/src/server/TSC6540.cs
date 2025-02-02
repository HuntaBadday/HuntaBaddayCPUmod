using LogicAPI.Server.Components;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using LogicLog;

namespace HuntaBaddayCPUmod {
    public class TSC6540 : LogicComponent {
        public override bool HasPersistentValues => true;
        
        const int BUSIN = 0;
        const int BUSOUT = 0;
        const int EPIN = 16;
        const int RPIN = 17;
        const int WPIN = 18;
        const int BUSADDR = 19;
        const int DEPIN = 30;
        const int RSPIN = 31;
        
        const int XOUT = 16;
        const int YOUT = 24;
        const int COUT = 32;
        const int SPIN = 48;
        const int FPIN = 49;
        
        const int TEXTADDR = 0x0000;
        const int FGCOLADDR = 0x0400;
        const int BGCOLADDR = 0x0800;
        const int EXCHARADDR = 0x0c00;
        
        // All these variables should be saved
        
        ushort[] vram = new ushort[0x10000];
        ushort[] screenBuffer = new ushort[0x10000];
        List<SBAction> drawBuffer = new List<SBAction>();
        
        ushort vramIOAddr;
        ushort screenBufferIOAddr;
        ushort textX;
        ushort textY;
        ushort textOX;
        ushort textOY;
        
        ushort resX;
        ushort resY;
        
        int gfxX;
        int gfxY;
        
        ushort charsetAddr;
        
        bool lastWPin;
        bool lastRPin;
        
        protected override void Initialize() {}
        
        private void setDefaultValues() {
            vramIOAddr = 0;
            screenBufferIOAddr = 0;
            textX = 32;
            textY = 32;
            textOX = 0;
            textOY = 0;
            resX = 256;
            resY = 256;
            charsetAddr = 0;
        }
        
        protected override void DoLogicUpdate() {
            Outputs[FPIN].On = false;
            doPixelDraw();
            
            if (Inputs[RSPIN].On) {
                drawBuffer.Clear();
                return;
            }
            
            ushort dataBusIn = readBus();
            int addressIn = readAddress();
            
            if (Inputs[EPIN].On && Inputs[WPIN].On && !lastWPin) {
                if (addressIn < 0x400) {
                    vram[TEXTADDR+addressIn] = dataBusIn;
                    updateScreenText(addressIn);
                } else {
                    doControlWrite(addressIn&0x3ff, dataBusIn);
                }
                QueueLogicUpdate();
            }
            
            if (Inputs[EPIN].On && Inputs[RPIN].On) {
                if (addressIn < 0x400) {
                    writeBus16(BUSOUT, vram[TEXTADDR+addressIn]);
                } else {
                    doControlRead(addressIn&0x3ff);
                }
            } else {
                writeBus16(BUSOUT, 0);
            }
            
            lastWPin = Inputs[WPIN].On;
            lastRPin = Inputs[RPIN].On;
        }
        
        private void doControlRead(int address) {
            switch (address) {
                case 0x00:
                    writeBus16(BUSOUT, vramIOAddr);
                    break;
                case 0x01:
                    writeBus16(BUSOUT, vram[vramIOAddr++]);
                    break;
                case 0x02:
                    writeBus16(BUSOUT, screenBufferIOAddr);
                    break;
                case 0x03:
                    writeBus16(BUSOUT, screenBuffer[screenBufferIOAddr++]);
                    break;
                case 0x04:
                    writeBus16(BUSOUT, resX);
                    break;
                case 0x05:
                    writeBus16(BUSOUT, resY);
                    break;
                case 0x06:
                    writeBus16(BUSOUT, textX);
                    break;
                case 0x07:
                    writeBus16(BUSOUT, textY);
                    break;
                case 0x08:
                    writeBus16(BUSOUT, textOX);
                    break;
                case 0x09:
                    writeBus16(BUSOUT, textOY);
                    break;
                case 0x0f:
                    writeBus16(BUSOUT, charsetAddr);
                    break;
            }
        }
        
        // Handle writes to non-screen memory area
        private void doControlWrite(int address, ushort data) {
            int x;
            int y;
            switch (address) {
                case 0x00:
                    vramIOAddr = data;
                    break;
                case 0x01:
                    vram[vramIOAddr] = data;
                    if (vramIOAddr >= FGCOLADDR && vramIOAddr <= FGCOLADDR+0x3ff) {
                        updateScreenText(vramIOAddr-FGCOLADDR);
                    } else if (vramIOAddr >= BGCOLADDR && vramIOAddr <= BGCOLADDR+0x3ff) {
                        updateScreenText(vramIOAddr-BGCOLADDR);
                    } else if (vramIOAddr >= TEXTADDR && vramIOAddr <= TEXTADDR+0x3ff) {
                        updateScreenText(data-TEXTADDR);
                    }
                    // Update the text area if character data is modified
                    if (vramIOAddr >= charsetAddr && vramIOAddr <= (ushort)(charsetAddr+0xff) && charsetAddr != 0)
                        redrawText();
                    vramIOAddr++;
                    break;
                case 0x02:
                    screenBufferIOAddr = data;
                    break;
                case 0x03:
                    if (screenBufferIOAddr >= resX*resY)
                        break;
                    x = (screenBufferIOAddr % textX);
                    y = (screenBufferIOAddr / textX);
                    drawPixel(x, y, data, false);
                    screenBufferIOAddr++;
                    break;
                case 0x04:
                    resX = data != 0 ? data : (ushort)1;
                    break;
                case 0x05:
                    resY = data != 0 ? data : (ushort)1;
                    break;
                case 0x06:
                    textX = data != 0 ? data : (ushort)1;
                    redrawText();
                    break;
                case 0x07:
                    textY = data != 0 ? data : (ushort)1;
                    redrawText();
                    break;
                case 0x08:
                    textOX = data;
                    redrawText();
                    break;
                case 0x09:
                    textOY = data;
                    redrawText();
                    break;
                case 0x0a:
                    doControlRegWrite(data);
                    break;
                case 0x0b:
                    if (data > textY || data == 0)
                        break;
                    for (y = 0; y < textY; y++) {
                        for (x = 0; x < textX; x++) {
                            ushort ctmp = 0x20;
                            ushort ftmp = vram[FGCOLADDR+(textY-1)*textX+x];
                            ushort btmp = vram[BGCOLADDR+(textY-1)*textX+x];
                            if (data+y < textY) {
                                ctmp = vram[TEXTADDR+(data+y)*textX + x];
                                ftmp = vram[FGCOLADDR+(data+y)*textX + x];
                                btmp = vram[BGCOLADDR+(data+y)*textX + x];
                            }
                            vram[TEXTADDR+y*textX+x] = ctmp;
                            vram[FGCOLADDR+y*textX+x] = ftmp;
                            vram[BGCOLADDR+y*textX+x] = btmp;
                            updateScreenText(y*textX+x);
                        }
                    }
                    break;
                case 0x0c:
                    gfxX = data&0xff;
                    gfxY = data >> 8;
                    break;
                case 0x0d:
                    drawGraphic(data, gfxX, gfxY);
                    break;
                case 0x0f:
                    charsetAddr = data;
                    redrawText();
                    break;
            }
        }
        
        // Handle a write to the control register
        private void doControlRegWrite(ushort data) {
            if ((data&0b1) != 0) {
                ushort fc = vram[FGCOLADDR];
                ushort bc = vram[BGCOLADDR];
                for (int i = 0; i < 0x10000; i++) {
                    screenBuffer[i] = bc;
                }
                for (int i = 0; i < textX*textY; i++) {
                    vram[TEXTADDR+i] = 0x20;
                    vram[FGCOLADDR+i] = fc;
                    vram[BGCOLADDR+i] = bc;
                }
                Outputs[FPIN].On = true;
                Outputs[SPIN].On = true;
                writeBus16(COUT, bc);
                drawBuffer.Clear();
                charsetAddr = 0;
            }
            if ((data&0b10) != 0) {
                for (int i = 0; i < textX*textY; i++) {
                    vram[TEXTADDR+i] = 0x20;
                    updateScreenText(i);
                }
            }
            if ((data&0b100) != 0) {
                ushort fc = vram[FGCOLADDR];
                ushort bc = vram[BGCOLADDR];
                for (int i = 0; i < textX*textY; i++) {
                    vram[FGCOLADDR+i] = fc;
                    vram[BGCOLADDR+i] = bc;
                    updateScreenText(i);
                }
            }
            if ((data & 0b10000000) != 0) {
                for (int y = 0; y < resX; y++) {
                    for (int x = 0; x < resX; x++) {
                        int index = XYtoIndex(x, y);
                        ushort col = screenBuffer[index];
                        drawPixel(x, y, col, true);
                    }
                }
                writeBus16(COUT, 0);
                Outputs[SPIN].On = true;
                Outputs[FPIN].On = true;
            }
        }
        
        // Display a graphic at x, y
        private void drawGraphic(ushort defVector, int x, int y) {
            int width = vram[defVector];
            int height = vram[(defVector+1)&0xffff];
            int dataVec = vram[(defVector+2)&0xffff];
            
            int i = 0;
            for (int yc = 0; yc < height; yc++) {
                for (int xc = 0; xc < width; xc++) {
                    drawPixel(xc+x, yc+y, vram[dataVec+i], false);
                    i++;
                }
            }
        }
        
        // Redraw all characters on screen
        private void redrawText() {
            for (int i = 0; i < textX*textY; i++) {
                updateScreenText(i);
            }
        }
        
        // Update character at position on the screen
        private void updateScreenText(int index) {
            if (index >= textX*textY)
                return;
            
            ushort foreground = vram[FGCOLADDR+index];
            ushort background = vram[BGCOLADDR+index];
            
            int x = (index % textX) * 8 + textOX;
            int y = (index / textX) * 8 + textOY;
            
            drawCharacter((byte)vram[TEXTADDR+index], x, y, foreground, background);
        }
        
        // Create all actions necessary to draw a character
        private void drawCharacter(byte character, int x, int y, ushort foreground, ushort background) {
            // Check if the default character set should be used
            if (charsetAddr == 0) {
                // Check if the character is normal, or should gbe gotten from vram
                if (character < 128) {
                    for (int yc = 0; yc < 8; yc++) {
                        byte line = tsc6540font.data[character,yc];
                        for (int xc = 0; xc < 8; xc++) {
                            if ((line & 0x80) != 0) {
                                drawPixel(x+xc, y+yc, foreground, false);
                            } else {
                                drawPixel(x+xc, y+yc, background, false);
                            }
                            line <<= 1;
                        }
                    }
                } else {
                    for (int yc = 0; yc < 8; yc++) {
                        byte line = (byte)vram[EXCHARADDR+(character-128)*8 + yc];
                        for (int xc = 0; xc < 8; xc++) {
                            if ((line & 0x80) != 0) {
                                drawPixel(x+xc, y+yc, foreground, false);
                            } else {
                                drawPixel(x+xc, y+yc, background, false);
                            }
                            line <<= 1;
                        }
                    }
                }
            } else {
                // Draw from character in vram
                for (int yc = 0; yc < 8; yc++) {
                    byte line = (byte)vram[charsetAddr+character*8 + yc];
                    for (int xc = 0; xc < 8; xc++) {
                        if ((line & 0x80) != 0) {
                            drawPixel(x+xc, y+yc, foreground, false);
                        } else {
                            drawPixel(x+xc, y+yc, background, false);
                        }
                        line <<= 1;
                    }
                }
            }
        }
        
        // Draw a pixel to the screen
        private void drawPixel(int x, int y, ushort colour, bool forceDraw) {
            if (drawBuffer.Count > 0x40000) {
                return;
            }
            int index = XYtoIndex(x, y);
            if ((screenBuffer[index] != colour) && (x >= 0) && (y >= 0) && (x <= 255) && (y <= 255) || forceDraw && (screenBuffer[index] != 0)) {
                screenBuffer[index] = colour;
                SBAction newAction = new SBAction();
                newAction.x = (byte)x;
                newAction.y = (byte)y;
                newAction.colour = colour;
                drawBuffer.Add(newAction);
            }
        }
        
        // Takes an action from the queue and sets the screen outputs accordingly
        private void doPixelDraw() {
            if (drawBuffer.Count == 0 || !Inputs[DEPIN].On) {
                Outputs[SPIN].On = false;
            } else {
                SBAction nextAction = drawBuffer[0];
                
                writeBus8(XOUT, nextAction.x);
                writeBus8(YOUT, nextAction.y);
                writeBus16(COUT, nextAction.colour);
                Outputs[SPIN].On = true;
                QueueLogicUpdate();
                
                drawBuffer.RemoveAt(0);
            }
        }
        
        // Convert X and Y to an index
        private int XYtoIndex(int x, int y) {
            return y*256 + x;
        }
        
        private void writeBus8(int startPin, byte value) {
            for (int i = 7; i >= 0; i--) {
                Outputs[startPin+i].On = (value & 1) == 1;
                value >>= 1;
            }
        }
        
        private void writeBus16(int startPin, ushort value) {
            for (int i = 15; i >= 0; i--) {
                Outputs[startPin+i].On = (value & 1) == 1;
                value >>= 1;
            }
        }
        
        private ushort readBus() {
            int output = 0;
            for (int i = 0; i < 16; i++) {
                output <<= 1;
                output |= Inputs[BUSIN+i].On ? 1 : 0;
            }
            return (ushort)output;
        }
        
        private int readAddress() {
            int output = 0;
            for (int i = 0; i < 11; i++) {
                output <<= 1;
                output |= Inputs[BUSADDR+i].On ? 1 : 0;
            }
            return output;
        }
        
        private struct SBAction {
            public byte x;
            public byte y;
            public ushort colour;
        }
        
        protected override byte[] SerializeCustomData(){
            MemoryStream memStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memStream);
            
            writer.Write(vramIOAddr);
            writer.Write(screenBufferIOAddr);
            writer.Write(textX);
            writer.Write(textY);
            writer.Write(textOX);
            writer.Write(textOY);
            writer.Write(resX);
            writer.Write(resY);
            writer.Write(gfxX);
            writer.Write(gfxY);
            writer.Write(charsetAddr);
            writer.Write(lastWPin);
            writer.Write(lastRPin);
            
            for (int i = 0; i < vram.Length; i++) {
                writer.Write(vram[i]);
            }
            for (int i = 0; i < screenBuffer.Length; i++) {
                writer.Write(screenBuffer[i]);
            }
            
            writer.Write(drawBuffer.Count);
            for (int i = 0; i < drawBuffer.Count; i++) {
                writer.Write(drawBuffer[i].x);
                writer.Write(drawBuffer[i].y);
                writer.Write(drawBuffer[i].colour);
            }
            
            writer.Flush();
            
            memStream.Seek(0, SeekOrigin.Begin);
            MemoryStream outputStream = new MemoryStream();
            DeflateStream compressor = new DeflateStream(outputStream, CompressionLevel.SmallestSize);
            compressor.Write(memStream.ToArray(), 0, (int)memStream.Length);
            compressor.Dispose();
            
            byte[] data = outputStream.ToArray();
            outputStream.Dispose();
            writer.Dispose();
            
            return data;
        }
        
        protected override void DeserializeData(byte[] data){
            if (data == null) {
                setDefaultValues();
                return;
            }
            
            MemoryStream dataStream = new MemoryStream(data);
            MemoryStream outputStream = new MemoryStream();
            DeflateStream decompressor = new DeflateStream(dataStream, CompressionMode.Decompress, false);
            decompressor.CopyTo(outputStream);
            decompressor.Dispose();
            
            outputStream.Seek(0, SeekOrigin.Begin);
            BinaryReader reader = new BinaryReader(outputStream);
            try {
                vramIOAddr = reader.ReadUInt16();
                screenBufferIOAddr = reader.ReadUInt16();
                textX = reader.ReadUInt16();
                textY = reader.ReadUInt16();
                textOX = reader.ReadUInt16();
                textOY = reader.ReadUInt16();
                resX = reader.ReadUInt16();
                resY = reader.ReadUInt16();
                gfxX = reader.ReadInt32();
                gfxY = reader.ReadInt32();
                charsetAddr = reader.ReadUInt16();
                lastWPin = reader.ReadBoolean();
                lastRPin = reader.ReadBoolean();
                
                for (int i = 0; i < vram.Length; i++) {
                    vram[i] = reader.ReadUInt16();
                }
                for (int i = 0; i < screenBuffer.Length; i++) {
                    screenBuffer[i] = reader.ReadUInt16();
                }
                
                int drawBufferCount = reader.ReadInt32();
                if (drawBufferCount > 0) {
                    for (int i = 0; i < drawBufferCount; i++) {
                        SBAction action = new SBAction();
                        action.x = reader.ReadByte();
                        action.y = reader.ReadByte();
                        action.colour = reader.ReadUInt16();
                        drawBuffer.Add(action);
                    }
                }
            } catch(EndOfStreamException) {
                Logger.Info("TSC6540: Invalid Customdata");
            }
            reader.Dispose();
        }
    }
}