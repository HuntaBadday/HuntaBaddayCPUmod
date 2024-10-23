#subruledef reg {
	r0	=> 1`4
	r1	=> 2`4
	r2	=> 3`4
	r3	=> 4`4
	r4	=> 5`4
	r5	=> 6`4
	r6	=> 7`4
	r7	=> 8`4
	r8	=> 9`4
	r9	=> 10`4
	r10	=> 11`4
	r11	=> 12`4
	r12	=> 13`4
	rr	=> 13`4
	sp	=> 14`4
	st	=> 15`4
}

#ruledef {
	brk =>  0x0000
	jmp	{op1: reg}	=> 0b0001 @ op1 @ 0b00000000
	jmp	{addr: u16}	=> 0b0001000000000000 @ addr
	
	mov	{op1: reg}, {op2: reg}	=> 0b0010 @ op1 @ op2 @ 0b0000
	mov	{op1: reg}, {imm: i16}	=> 0b0010 @ op1 @ 0b00000000 @ imm
	
	lod	{op1: reg}, {op2: reg}	=> 0b0011 @ op1 @ op2 @ 0b0000
	lod	{op1: reg}, {addr: u16}	=> 0b0011 @ op1 @ 0b00000000 @ addr
	lod {op1: reg}, {op2: reg}+{op3: reg}	=> 0b0011 @ op1 @ op2 @ op3
	lod {op1: reg}, {op3: reg}+{offst: i16}	=> 0b0011 @ op1 @ 0b0000 @ op3 @ offst
	lod {op1: reg}, {offst: i16}+{op3: reg}	=> 0b0011 @ op1 @ 0b0000 @ op3 @ offst
	
	sto	{op1: reg}, {op2: reg}	=> 0b0100 @ op1 @ op2 @ 0b0000
	sto	{op1: reg}, {addr: u16}	=> 0b0100 @ op1 @ 0b00000000 @ addr
	sto {op1: reg}, {op2: reg}+{op3: reg}	=> 0b0100 @ op1 @ op2 @ op3
	sto	{op1: reg}, {op3: reg}+{offst: i16}	=> 0b0100 @ op1 @ 0b0000 @ op3 @ offst
	sto {op1: reg}, {offst: i16}+{op3: reg}	=> 0b0100 @ op1 @ 0b0000 @ op3 @ offst
	;-------------------
	
	mov	{op1: reg}, [{op2: reg}]	=> 0b0011 @ op1 @ op2 @ 0b0000
	mov	{op1: reg}, [{addr: u16}]	=> 0b0011 @ op1 @ 0b00000000 @ addr
	mov {op1: reg}, [{op2: reg}+{op3: reg}]	=> 0b0011 @ op1 @ op2 @ op3
	mov {op1: reg}, [{op3: reg}+{offst: i16}]	=> 0b0011 @ op1 @ 0b0000 @ op3 @ offst
	mov {op1: reg}, [{offst: i16}+{op3: reg}]	=> 0b0011 @ op1 @ 0b0000 @ op3 @ offst
	
	mov	[{op2: reg}], {op1: reg}	=> 0b0100 @ op1 @ op2 @ 0b0000
	mov	[{addr: u16}], {op1: reg}	=> 0b0100 @ op1 @ 0b00000000 @ addr
	mov [{op2: reg}+{op3: reg}], {op1: reg}	=> 0b0100 @ op1 @ op2 @ op3
	mov	[{op3: reg}+{offst: i16}], {op1: reg}	=> 0b0100 @ op1 @ 0b0000 @ op3 @ offst
	mov	[{op3: reg}-{offst: i16}], {op1: reg}	=> 0b0100 @ op1 @ 0b0000 @ op3 @ offst
	mov [{offst: i16}+{op3: reg}], {op1: reg} 	=> 0b0100 @ op1 @ 0b0000 @ op3 @ offst
	
	;-------------------
	add {op1: reg}, {op2: reg}	=> 0b0101 @ op1 @ op2 @ 0b0000
	add {op1: reg}, {imm: i16}	=> 0b0101 @ op1 @ 0b00000000 @ imm
	add {imm: i16}, {op2: reg}	=> 0b01010000 @ op2 @ 0b0000 @ imm
	
	adc {op1: reg}, {op2: reg}	=> 0b0101 @ op1 @ op2 @ 0b0001
	adc {op1: reg}, {imm: i16}	=> 0b0101 @ op1 @ 0b00000001 @ imm
	adc {imm: i16}, {op2: reg}	=> 0b01010000 @ op2 @ 0b0001 @ imm
	
	sub {op1: reg}, {op2: reg}	=> 0b0101 @ op1 @ op2 @ 0b0010
	sub {op1: reg}, {imm: i16}	=> 0b0101 @ op1 @ 0b00000010 @ imm
	sub {imm: i16}, {op2: reg}	=> 0b01010000 @ op2 @ 0b0010 @ imm
	
	sbc {op1: reg}, {op2: reg}	=> 0b0101 @ op1 @ op2 @ 0b0011
	sbc {op1: reg}, {imm: i16}	=> 0b0101 @ op1 @ 0b00000011 @ imm
	sbc {imm: i16}, {op2: reg}	=> 0b01010000 @ op2 @ 0b0011 @ imm
	
	shl {op1: reg}	=> 0b0101 @ op1 @ 0b00000100
	rol {op1: reg}	=> 0b0101 @ op1 @ 0b00000101
	shr {op1: reg}	=> 0b0101 @ op1 @ 0b00000110
	ror {op1: reg}	=> 0b0101 @ op1 @ 0b00000111
	
	and {op1: reg}, {op2: reg}	=> 0b0101 @ op1 @ op2 @ 0b1000
	and {op1: reg}, {imm: i16}	=> 0b0101 @ op1 @ 0b00001000 @ imm
	and {imm: i16}, {op2: reg}	=> 0b01010000 @ op2 @ 0b1000 @ imm
	
	or {op1: reg}, {op2: reg}	=> 0b0101 @ op1 @ op2 @ 0b1001
	or {op1: reg}, {imm: i16}	=> 0b0101 @ op1 @ 0b00001001 @ imm
	or {imm: i16}, {op2: reg}	=> 0b01010000 @ op2 @ 0b1001 @ imm
	
	xor {op1: reg}, {op2: reg}	=> 0b0101 @ op1 @ op2 @ 0b1010
	xor {op1: reg}, {imm: i16}	=> 0b0101 @ op1 @ 0b00001010 @ imm
	xor {imm: i16}, {op2: reg}	=> 0b01010000 @ op2 @ 0b1010 @ imm
	
	mul {op1: reg}, {op2: reg}	=> 0b0101 @ op1 @ op2 @ 0b1011
	mul {op1: reg}, {imm: i16}	=> 0b0101 @ op1 @ 0b00001011 @ imm
	mul {imm: i16}, {op2: reg}	=> 0b01010000 @ op2 @ 0b1011 @ imm
	
	smul {op1: reg}, {op2: reg}	=> 0b0101 @ op1 @ op2 @ 0b1100
	smul {op1: reg}, {imm: i16}	=> 0b0101 @ op1 @ 0b00001100 @ imm
	smul {imm: i16}, {op2: reg}	=> 0b01010000 @ op2 @ 0b1100 @ imm
	
	div {op1: reg}, {op2: reg}	=> 0b0101 @ op1 @ op2 @ 0b1101
	div {op1: reg}, {imm: i16}	=> 0b0101 @ op1 @ 0b00001101 @ imm
	div {imm: i16}, {op2: reg}	=> 0b01010000 @ op2 @ 0b1101 @ imm
	
	sdiv {op1: reg}, {op2: reg}	=> 0b0101 @ op1 @ op2 @ 0b1110
	sdiv {op1: reg}, {imm: i16}	=> 0b0101 @ op1 @ 0b00001110 @ imm
	sdiv {imm: i16}, {op2: reg}	=> 0b01010000 @ op2 @ 0b1110 @ imm
	
	neg {op1: reg}	=>	0b0101 @ op1 @ 0b00001111
	
	;-------------------
	
	cmp {op1: reg}, {op2: reg}	=> 0b0110 @ op1 @ op2 @ 0b0000
	cmp {op1: reg}, {imm: i16}	=> 0b0110 @ op1 @ 0b00000000 @ imm
	cmp {imm: i16}, {op2: reg}	=> 0b01100000 @ op2 @ 0b0000 @ imm
	
	;-------------------
	bcs {op1: reg}	=> 0b0111 @ op1 @ 0b00000001
	bcs {addr: u16}	=> 0b0111 @ 0b000000000001 @ addr
	
	beq {op1: reg}	=> 0b0111 @ op1 @ 0b00000010
	beq {addr: u16}	=> 0b0111 @ 0b000000000010 @ addr
	
	bmi {op1: reg}	=> 0b0111 @ op1 @ 0b00000100
	bmi {addr: u16}	=> 0b0111 @ 0b000000000100 @ addr
	
	bcc {op1: reg}	=> 0b0111 @ op1 @ 0b00001001
	bcc {addr: u16}	=> 0b0111 @ 0b000000001001 @ addr
	
	bne {op1: reg}	=> 0b0111 @ op1 @ 0b00001010
	bne {addr: u16}	=> 0b0111 @ 0b000000001010 @ addr
	
	bpl {op1: reg}	=> 0b0111 @ op1 @ 0b00001100
	bpl {addr: u16}	=> 0b0111 @ 0b000000001100 @ addr
	;-------------------
	
	nop	=> 0b1000 @ 0b000000000000
	
	int	{op1: reg}	=> 0b1001 @ op1 @ 0b00000000
	int	{addr: u16}	=> 0b1001 @ 0b000000000000 @ addr
	cli	=> 0b1001000000000010
	sei	=> 0b1001000000000001
	
	plp	=> 0b1010000000000000
	
	push {op1: reg}	=> 0b1011 @ op1 @ 0b00000000
	push {imm: i16}	=> 0b1011000000000000 @ imm
	
	pop {op1: reg}	=> 0b1100 @ op1 @ 0b00000000
	
	jsr {op1: reg}	=> 0b1101 @ op1 @ 0b00000000
	jsr {addr: u16}	=> 0b1101 @ 0b000000000000 @ addr
	
	rts	=> 0b1110000000000000
	rti	=> 0b1111000000000000
}

#ruledef {
	pushall	=>	asm{
		push st
		push r0
		push r1
		push r2
		push r3
		push r4
		push r5
		push r6
		push r7
		push r8
		push r9
		push r10
		push r11
		push r12
	}
	popall	=>	asm{
		pop r12
		pop r11
		pop r10
		pop r9
		pop r8
		pop r7
		pop r6
		pop r5
		pop r4
		pop r3
		pop r2
		pop r1
		pop r0
		plp
	}
	clc => asm{
		and st, 0b1111111111111110
	}
	sec => asm{
		or st, 0b001
	}
}