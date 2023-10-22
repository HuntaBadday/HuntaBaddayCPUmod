#subruledef reg {
	r0	=> 1`3
	r1	=> 2`3
	r2	=> 3`3
	r3	=> 4`3
	r4	=> 5`3
	sp	=> 6`3
	st	=> 7`3
}

#ruledef {
	brk =>  0x0000
	jmp	{op1: reg}	=> 0b0001 @ op1 @ 0b000000000
	jmp	{addr: u16}	=> 0b0001000000000000 @ addr
	
	mov	{op1: reg}, {op2: reg}	=> 0b0010 @ op1 @ op2 @ 0b000000
	mov	{op1: reg}, {imm: i16}	=> 0b0010 @ op1 @ 0b000000000 @ imm
	
	lod	{op1: reg}, {op2: reg}	=> 0b0011 @ op1 @ op2 @ 0b000000
	lod	{op1: reg}, {addr: u16}	=> 0b0011 @ op1 @ 0b000000000 @ addr
	lod {op1: reg}, {op2: reg}+{op3: reg}	=> 0b0011 @ op1 @ op2 @ 0b1 @ op3 @ 0b00
	lod {op1: reg}, {op3: reg}+{offst: i16}	=> 0b0011 @ op1 @ 0b000 @ 0b1 @ op3 @ 0b00 @ offst
	lod {op1: reg}, {offst: i16}+{op3: reg}	=> 0b0011 @ op1 @ 0b000 @ 0b1 @ op3 @ 0b00 @ offst
	
	sto	{op1: reg}, {op2: reg}	=> 0b0100 @ op1 @ op2 @ 0b000000
	sto	{op1: reg}, {addr: u16}	=> 0b0100 @ op1 @ 0b000000000 @ addr
	sto {op1: reg}, {op2: reg}+{op3: reg}	=> 0b0100 @ op1 @ op2 @ 0b1 @ op3 @ 0b00
	sto	{op1: reg}, {op3: reg}+{offst: i16}	=> 0b0100 @ op1 @ 0b000 @ 0b1 @ op3 @ 0b00 @ offst
	sto {op1: reg}, {offst: i16}+{op3: reg}	=> 0b0100 @ op1 @ 0b000 @ 0b1 @ op3 @ 0b00 @ offst
	;-------------------
	
	mov	{op1: reg}, [{op2: reg}]	=> 0b0011 @ op1 @ op2 @ 0b000000
	mov	{op1: reg}, [{addr: u16}]	=> 0b0011 @ op1 @ 0b000000000 @ addr
	mov {op1: reg}, [{op2: reg}+{op3: reg}]	=> 0b0011 @ op1 @ op2 @ 0b1 @ op3 @ 0b00
	mov {op1: reg}, [{op3: reg}+{offst: i16}]	=> 0b0011 @ op1 @ 0b000 @ 0b1 @ op3 @ 0b00 @ offst
	mov {op1: reg}, [{offst: i16}+{op3: reg}]	=> 0b0011 @ op1 @ 0b000 @ 0b1 @ op3 @ 0b00 @ offst
	
	mov	[{op2: reg}], {op1: reg}	=> 0b0100 @ op1 @ op2 @ 0b000000
	mov	[{addr: u16}], {op1: reg}	=> 0b0100 @ op1 @ 0b000000000 @ addr
	mov [{op2: reg}+{op3: reg}], {op1: reg}	=> 0b0100 @ op1 @ op2 @ 0b1 @ op3 @ 0b00
	mov	[{op3: reg}+{offst: i16}], {op1: reg}	=> 0b0100 @ op1 @ 0b000 @ 0b1 @ op3 @ 0b00 @ offst
	mov [{offst: i16}+{op3: reg}], {op1: reg} 	=> 0b0100 @ op1 @ 0b000 @ 0b1 @ op3 @ 0b00 @ offst
	
	;-------------------
	add {op1: reg}, {op2: reg}	=> 0b0101 @ op1 @ op2 @ 0b000000
	add {op1: reg}, {imm: i16}	=> 0b0101 @ op1 @ 0b000000000 @ imm
	add {imm: i16}, {op2: reg}	=> 0b0101000 @ op2 @ 0b000000 @ imm
	
	adc {op1: reg}, {op2: reg}	=> 0b0101 @ op1 @ op2 @ 0b000100
	adc {op1: reg}, {imm: i16}	=> 0b0101 @ op1 @ 0b000000100 @ imm
	adc {imm: i16}, {op2: reg}	=> 0b0101000 @ op2 @ 0b000100 @ imm
	
	sub {op1: reg}, {op2: reg}	=> 0b0101 @ op1 @ op2 @ 0b001000
	sub {op1: reg}, {imm: i16}	=> 0b0101 @ op1 @ 0b000001000 @ imm
	sub {imm: i16}, {op2: reg}	=> 0b0101000 @ op2 @ 0b001000 @ imm
	
	sbc {op1: reg}, {op2: reg}	=> 0b0101 @ op1 @ op2 @ 0b001100
	sbc {op1: reg}, {imm: i16}	=> 0b0101 @ op1 @ 0b000001100 @ imm
	sbc {imm: i16}, {op2: reg}	=> 0b0101000 @ op2 @ 0b001100 @ imm
	
	shl {op1: reg}	=> 0b0101 @ op1 @ 0b000010000
	rol {op1: reg}	=> 0b0101 @ op1 @ 0b000010100
	shr {op1: reg}	=> 0b0101 @ op1 @ 0b000011000
	ror {op1: reg}	=> 0b0101 @ op1 @ 0b000011100
	
	and {op1: reg}, {op2: reg}	=> 0b0101 @ op1 @ op2 @ 0b100000
	and {op1: reg}, {imm: i16}	=> 0b0101 @ op1 @ 0b000100000 @ imm
	and {imm: i16}, {op2: reg}	=> 0b0101000 @ op2 @ 0b100000 @ imm
	
	or {op1: reg}, {op2: reg}	=> 0b0101 @ op1 @ op2 @ 0b100100
	or {op1: reg}, {imm: i16}	=> 0b0101 @ op1 @ 0b000100100 @ imm
	or {imm: i16}, {op2: reg}	=> 0b0101000 @ op2 @ 0b100100 @ imm
	
	xor {op1: reg}, {op2: reg}	=> 0b0101 @ op1 @ op2 @ 0b101000
	xor {op1: reg}, {imm: i16}	=> 0b0101 @ op1 @ 0b000101000 @ imm
	xor {imm: i16}, {op2: reg}	=> 0b0101000 @ op2 @ 0b101000 @ imm
	;-------------------
	
	cmp {op1: reg}, {op2: reg}	=> 0b0110 @ op1 @ op2 @ 0b000000
	cmp {op1: reg}, {imm: i16}	=> 0b0110 @ op1 @ 0b000000000 @ imm
	cmp {imm: i16}, {op2: reg}	=> 0b0110000 @ op2 @ 0b000000 @ imm
	
	;-------------------
	bcs {op1: reg}	=> 0b0111 @ op1 @ 0b000000100
	bcs {addr: u16}	=> 0b0111 @ 0b000000000100 @ addr
	
	beq {op1: reg}	=> 0b0111 @ op1 @ 0b000001000
	beq {addr: u16}	=> 0b0111 @ 0b000000001000 @ addr
	
	bmi {op1: reg}	=> 0b0111 @ op1 @ 0b000010000
	bmi {addr: u16}	=> 0b0111 @ 0b000000010000 @ addr
	
	bcc {op1: reg}	=> 0b0111 @ op1 @ 0b000100100
	bcc {addr: u16}	=> 0b0111 @ 0b000000100100 @ addr
	
	bne {op1: reg}	=> 0b0111 @ op1 @ 0b000101000
	bne {addr: u16}	=> 0b0111 @ 0b000000101000 @ addr
	
	bpl {op1: reg}	=> 0b0111 @ op1 @ 0b000110000
	bpl {addr: u16}	=> 0b0111 @ 0b000000110000 @ addr
	;-------------------
	
	nop	=> 0b1000 @ 0b000000000000
	
	int	{op1: reg}	=> 0b1001 @ op1 @ 0b000000000
	int	{addr: u16}	=> 0b1001 @ 0b000000000000 @ addr
	cli	=> 0b1001000000001000
	sei	=> 0b1001000000000100
	
	plp	=> 0b1010000000000000
	
	push {op1: reg}	=> 0b1011 @ op1 @ 0b000000000
	push {imm: i16}	=> 0b1011000000000000 @ imm
	
	pop {op1: reg}	=> 0b1100 @ op1 @ 0b000000000
	
	jsr {op1: reg}	=> 0b1101 @ op1 @ 0b000000000
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
	}
	popall	=>	asm{
		pop r4
		pop r3
		pop r2
		pop r1
		pop r0
		plp
	}
	pushregs	=>	asm{
		push r0
		push r1
		push r2
		push r3
		push r4
	}
	popregs	=>	asm{
		pop r4
		pop r3
		pop r2
		pop r1
		pop r0
	}
}

#bankdef lwc31bank {
	#bits 16
	#addr 0
	#size 0x10000
	#outp 0
}
#bank lwc31bank