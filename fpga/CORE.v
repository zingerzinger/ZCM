module CORE (
    input  wire clk_in,
	 	 	 
	 input  wire [7:0]  in_uart_data,
	 output wire [7:0] out_uart_data,
	 
	 input  wire  in_uart_rd_ready,
	 input  wire  in_uart_wr_busy,
	 
	 output wire  out_uart_wr,
	 output wire  out_uart_rst,
	 
	 output wire [31:0] adr,		 
	 inout  wire [31:0] dio,
	 output wire        mwr
);

// === === ===
localparam integer IN_FREQ = 10000000;
localparam integer STARTUP_HALT_CYCLES = IN_FREQ / 10;
localparam integer MILLIS_DIV = IN_FREQ / 1000;
// === === ===

assign dio = WR_MEM ? MEM_OUT : 32'bzzzz_zzzz_zzzz_zzzz_zzzz_zzzz_zzzz_zzzz;
assign adr = DIO_ADDR;
reg WR_MEM = 0;
integer MEM_OUT;
assign mwr = WR_MEM;

reg WR_UART = 0;
reg [7:0] uart_tx_data;
assign out_uart_wr = WR_UART;
assign out_uart_data = uart_tx_data;

reg RST_READ_UART = 0;
assign out_uart_rst = RST_READ_UART;

integer DIO_ADDR = 0;

integer REG_I_PTR = 0; // instruction pointer
integer REG_INSTRUCTION = 69; // current instruction
integer EXEC_COUNTER = 0;

integer MILLIS = 0;
integer MILLIS_DIV_COUNTER = 0;

integer REG_R = 0;
integer REG_A = 0;
integer REG_B = 0;

integer REG_SP = 0;

integer REG_SLEEP = 0;

initial begin
	//REG_R = "A";
	REG_R = "M";
end

always @(posedge clk_in) begin

// MILLIS TIMER

	MILLIS_DIV_COUNTER <= MILLIS_DIV_COUNTER + 1;
	if (MILLIS_DIV_COUNTER >= MILLIS_DIV) begin
		MILLIS_DIV_COUNTER <= 0;
		MILLIS <= MILLIS + 1;
	end
	
// INSTRUCTION EXECUTION COUNTER

	EXEC_COUNTER <= EXEC_COUNTER + 1;

// INSTRUCTION EXECUTION IMPLEMENTATION
	
	case (REG_INSTRUCTION)
//==============================================================================	
	  69 /* STARTUP */ : begin
			if (EXEC_COUNTER > STARTUP_HALT_CYCLES) begin
				EXEC_COUNTER    <= 0;
				REG_INSTRUCTION <= dio;
			end
		end
//==============================================================================		
		0 /* HLT */ : begin
		// do nothing
		end
//==============================================================================		
		1 /* NOP */ : begin
			// load next instruction
				
			case (EXEC_COUNTER)
				0: begin
					REG_I_PTR <= REG_I_PTR + 1;
					DIO_ADDR  <= REG_I_PTR + 1;
				end
				1: begin /* wait cycle */ end
				2: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
			endcase
		end
//==============================================================================		
		2 /* PUTC */ : begin
		
			case (EXEC_COUNTER)	
				0: begin
					if (in_uart_wr_busy) begin EXEC_COUNTER <= 0; end // wait uart tx ready
				end
				1: begin
					uart_tx_data <= (REG_R == 0) ? "\n" /*linefeed*/ : REG_R;
					WR_UART   <= 1;
					REG_I_PTR <= REG_I_PTR + 1;
					DIO_ADDR  <= REG_I_PTR + 1;
				end
				2: begin /* wait cycle */ WR_UART <= 0; end
				3: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
			endcase
		end
//==============================================================================		
		3 /* PRINT */ : begin
			// idle, not implemented
		end
//==============================================================================		
		4 /* READC */ : begin
		
			case (EXEC_COUNTER)	
				0: begin
					if (!in_uart_rd_ready) begin EXEC_COUNTER <= 0; end // wait uart rx ready
				end
				1: begin
					REG_R         <= in_uart_data;
					RST_READ_UART <= 1;
					
					REG_I_PTR     <= REG_I_PTR + 1;
					DIO_ADDR      <= REG_I_PTR + 1;
				end
				2: begin /* wait cycle */ RST_READ_UART <= 0; end
				3: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
			endcase
		end
//==============================================================================		
		5 /* READI */ : begin
			// idle, not implemented
		end
//==============================================================================		
		6 /* SLEEP */ : begin
		
			case (EXEC_COUNTER)	
				0: begin
					REG_SLEEP <= MILLIS;
				end
				1: begin
					if (MILLIS < (REG_SLEEP + REG_R)) begin EXEC_COUNTER <= 1; end // sleep
				end
				2: begin					
					REG_I_PTR     <= REG_I_PTR + 1;
					DIO_ADDR      <= REG_I_PTR + 1;
				end
				3: begin /* wait cycle */ end
				4: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
			endcase
		end
//==============================================================================	
		7 /* TIM */ : begin
		
			case (EXEC_COUNTER)	
				0: begin
					REG_R <= MILLIS;
				end
				1: begin					
					REG_I_PTR     <= REG_I_PTR + 1;
					DIO_ADDR      <= REG_I_PTR + 1;
				end
				3: begin /* wait cycle */ end
				4: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
			endcase
		end
//==============================================================================	
//	(MULTILINE REFERENCE VERSION) 8 /* MUL */ : begin
//		
//			case (EXEC_COUNTER)	
//				0: begin DIO_ADDR <= REG_SP + 2; end
//				1: begin /* wait cycle */ end
//				2: begin REG_A <= dio; end
//				
//				3: begin DIO_ADDR <= REG_SP + 1; end
//				4: begin /* wait cycle */ end
//				5: begin REG_B <= dio; end
//				
//				6: begin REG_R <= REG_A * REG_B; REG_SP <= REG_SP + 2; end
//				
//				7: begin DIO_ADDR <= REG_SP; MEM_OUT <= REG_R; WR_MEM <= 1; REG_SP <= REG_SP - 1; end
//				
//				8: begin WR_MEM <= 0; /* wait cycle */ end
//				
//				9: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end
//			  10: begin /* wait cycle */ end
//			  11: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
//			endcase
//		end
//
//==============================================================================

// ARITHMETIC
//==============================================================================	
 8 /* MUL */ : begin case (EXEC_COUNTER) 0: begin DIO_ADDR <= REG_SP + 2; end 1: begin /* wait cycle */ end 2: begin REG_A <= dio; end 3: begin DIO_ADDR <= REG_SP + 1; end 4: begin /* wait cycle */ end 5: begin REG_B <= dio; end 6: begin    REG_R <= REG_A * REG_B; REG_SP <= REG_SP + 2;    end 7: begin DIO_ADDR <= REG_SP; MEM_OUT <= REG_R; WR_MEM <= 1; REG_SP <= REG_SP - 1; end 8: begin WR_MEM <= 0; /* wait cycle */ end 9: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end 10: begin /* wait cycle */ end 11: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end endcase end
 9 /* DIV */ : begin case (EXEC_COUNTER) 0: begin DIO_ADDR <= REG_SP + 2; end 1: begin /* wait cycle */ end 2: begin REG_A <= dio; end 3: begin DIO_ADDR <= REG_SP + 1; end 4: begin /* wait cycle */ end 5: begin REG_B <= dio; end 6: begin /* REG_R <= REG_A / REG_B; REG_SP <= REG_SP + 2; */ end 7: begin DIO_ADDR <= REG_SP; MEM_OUT <= REG_R; WR_MEM <= 1; REG_SP <= REG_SP - 1; end 8: begin WR_MEM <= 0; /* wait cycle */ end 9: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end 10: begin /* wait cycle */ end 11: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end endcase end		
10 /* REM */ : begin case (EXEC_COUNTER) 0: begin DIO_ADDR <= REG_SP + 2; end 1: begin /* wait cycle */ end 2: begin REG_A <= dio; end 3: begin DIO_ADDR <= REG_SP + 1; end 4: begin /* wait cycle */ end 5: begin REG_B <= dio; end 6: begin /* REG_R <= REG_A % REG_B; REG_SP <= REG_SP + 2; */ end 7: begin DIO_ADDR <= REG_SP; MEM_OUT <= REG_R; WR_MEM <= 1; REG_SP <= REG_SP - 1; end 8: begin WR_MEM <= 0; /* wait cycle */ end 9: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end 10: begin /* wait cycle */ end 11: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end endcase end
11 /* ADD */ : begin case (EXEC_COUNTER) 0: begin DIO_ADDR <= REG_SP + 2; end 1: begin /* wait cycle */ end 2: begin REG_A <= dio; end 3: begin DIO_ADDR <= REG_SP + 1; end 4: begin /* wait cycle */ end 5: begin REG_B <= dio; end 6: begin    REG_R <= REG_A + REG_B; REG_SP <= REG_SP + 2;    end 7: begin DIO_ADDR <= REG_SP; MEM_OUT <= REG_R; WR_MEM <= 1; REG_SP <= REG_SP - 1; end 8: begin WR_MEM <= 0; /* wait cycle */ end 9: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end 10: begin /* wait cycle */ end 11: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end endcase end
12 /* SUB */ : begin case (EXEC_COUNTER) 0: begin DIO_ADDR <= REG_SP + 2; end 1: begin /* wait cycle */ end 2: begin REG_A <= dio; end 3: begin DIO_ADDR <= REG_SP + 1; end 4: begin /* wait cycle */ end 5: begin REG_B <= dio; end 6: begin    REG_R <= REG_A - REG_B; REG_SP <= REG_SP + 2;    end 7: begin DIO_ADDR <= REG_SP; MEM_OUT <= REG_R; WR_MEM <= 1; REG_SP <= REG_SP - 1; end 8: begin WR_MEM <= 0; /* wait cycle */ end 9: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end 10: begin /* wait cycle */ end 11: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end endcase end

// LOGICAL
//==============================================================================
13 /* LESS    */ : begin case (EXEC_COUNTER) 0: begin DIO_ADDR <= REG_SP + 2; end 1: begin /* wait cycle */ end 2: begin REG_A <= dio; end 3: begin DIO_ADDR <= REG_SP + 1; end 4: begin /* wait cycle */ end 5: begin REG_B <= dio; end 6: begin    REG_R <= ( REG_A  < REG_B               ? 1:0); REG_SP <= REG_SP + 2;    end 7: begin DIO_ADDR <= REG_SP; MEM_OUT <= REG_R; WR_MEM <= 1; REG_SP <= REG_SP - 1; end 8: begin WR_MEM <= 0; /* wait cycle */ end 9: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end 10: begin /* wait cycle */ end 11: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end endcase end
14 /* LESSEQ  */ : begin case (EXEC_COUNTER) 0: begin DIO_ADDR <= REG_SP + 2; end 1: begin /* wait cycle */ end 2: begin REG_A <= dio; end 3: begin DIO_ADDR <= REG_SP + 1; end 4: begin /* wait cycle */ end 5: begin REG_B <= dio; end 6: begin    REG_R <= ( REG_A <= REG_B               ? 1:0); REG_SP <= REG_SP + 2;    end 7: begin DIO_ADDR <= REG_SP; MEM_OUT <= REG_R; WR_MEM <= 1; REG_SP <= REG_SP - 1; end 8: begin WR_MEM <= 0; /* wait cycle */ end 9: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end 10: begin /* wait cycle */ end 11: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end endcase end
15 /* GREAT   */ : begin case (EXEC_COUNTER) 0: begin DIO_ADDR <= REG_SP + 2; end 1: begin /* wait cycle */ end 2: begin REG_A <= dio; end 3: begin DIO_ADDR <= REG_SP + 1; end 4: begin /* wait cycle */ end 5: begin REG_B <= dio; end 6: begin    REG_R <= ( REG_A  > REG_B               ? 1:0); REG_SP <= REG_SP + 2;    end 7: begin DIO_ADDR <= REG_SP; MEM_OUT <= REG_R; WR_MEM <= 1; REG_SP <= REG_SP - 1; end 8: begin WR_MEM <= 0; /* wait cycle */ end 9: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end 10: begin /* wait cycle */ end 11: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end endcase end
16 /* GREATEQ */ : begin case (EXEC_COUNTER) 0: begin DIO_ADDR <= REG_SP + 2; end 1: begin /* wait cycle */ end 2: begin REG_A <= dio; end 3: begin DIO_ADDR <= REG_SP + 1; end 4: begin /* wait cycle */ end 5: begin REG_B <= dio; end 6: begin    REG_R <= ( REG_A >= REG_B               ? 1:0); REG_SP <= REG_SP + 2;    end 7: begin DIO_ADDR <= REG_SP; MEM_OUT <= REG_R; WR_MEM <= 1; REG_SP <= REG_SP - 1; end 8: begin WR_MEM <= 0; /* wait cycle */ end 9: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end 10: begin /* wait cycle */ end 11: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end endcase end
17 /* EQU     */ : begin case (EXEC_COUNTER) 0: begin DIO_ADDR <= REG_SP + 2; end 1: begin /* wait cycle */ end 2: begin REG_A <= dio; end 3: begin DIO_ADDR <= REG_SP + 1; end 4: begin /* wait cycle */ end 5: begin REG_B <= dio; end 6: begin    REG_R <= ( REG_A == REG_B               ? 1:0); REG_SP <= REG_SP + 2;    end 7: begin DIO_ADDR <= REG_SP; MEM_OUT <= REG_R; WR_MEM <= 1; REG_SP <= REG_SP - 1; end 8: begin WR_MEM <= 0; /* wait cycle */ end 9: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end 10: begin /* wait cycle */ end 11: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end endcase end
18 /* NEQ     */ : begin case (EXEC_COUNTER) 0: begin DIO_ADDR <= REG_SP + 2; end 1: begin /* wait cycle */ end 2: begin REG_A <= dio; end 3: begin DIO_ADDR <= REG_SP + 1; end 4: begin /* wait cycle */ end 5: begin REG_B <= dio; end 6: begin    REG_R <= ( REG_A != REG_B               ? 1:0); REG_SP <= REG_SP + 2;    end 7: begin DIO_ADDR <= REG_SP; MEM_OUT <= REG_R; WR_MEM <= 1; REG_SP <= REG_SP - 1; end 8: begin WR_MEM <= 0; /* wait cycle */ end 9: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end 10: begin /* wait cycle */ end 11: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end endcase end
19 /* AND     */ : begin case (EXEC_COUNTER) 0: begin DIO_ADDR <= REG_SP + 2; end 1: begin /* wait cycle */ end 2: begin REG_A <= dio; end 3: begin DIO_ADDR <= REG_SP + 1; end 4: begin /* wait cycle */ end 5: begin REG_B <= dio; end 6: begin    REG_R <= ( (REG_A != 0) && (REG_B != 0) ? 1:0); REG_SP <= REG_SP + 2;    end 7: begin DIO_ADDR <= REG_SP; MEM_OUT <= REG_R; WR_MEM <= 1; REG_SP <= REG_SP - 1; end 8: begin WR_MEM <= 0; /* wait cycle */ end 9: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end 10: begin /* wait cycle */ end 11: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end endcase end
20 /* OR      */ : begin case (EXEC_COUNTER) 0: begin DIO_ADDR <= REG_SP + 2; end 1: begin /* wait cycle */ end 2: begin REG_A <= dio; end 3: begin DIO_ADDR <= REG_SP + 1; end 4: begin /* wait cycle */ end 5: begin REG_B <= dio; end 6: begin    REG_R <= ( (REG_A != 0) || (REG_B != 0) ? 1:0); REG_SP <= REG_SP + 2;    end 7: begin DIO_ADDR <= REG_SP; MEM_OUT <= REG_R; WR_MEM <= 1; REG_SP <= REG_SP - 1; end 8: begin WR_MEM <= 0; /* wait cycle */ end 9: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end 10: begin /* wait cycle */ end 11: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end endcase end
21 /* NOT     */ : begin case (EXEC_COUNTER) 0: begin DIO_ADDR <= REG_SP + 2; end 1: begin /* wait cycle */ end 2: begin REG_A <= dio; end 3: begin DIO_ADDR <= REG_SP + 1; end 4: begin /* wait cycle */ end 5: begin REG_B <= dio; end 6: begin    REG_R <= ( (REG_A == 0)                 ? 1:0); REG_SP <= REG_SP + 1;    end 7: begin DIO_ADDR <= REG_SP; MEM_OUT <= REG_R; WR_MEM <= 1; REG_SP <= REG_SP - 1; end 8: begin WR_MEM <= 0; /* wait cycle */ end 9: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end 10: begin /* wait cycle */ end 11: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end endcase end

// STACK
//==============================================================================	
	22 /* SSP */ : begin
		
			case (EXEC_COUNTER)	
				0: begin REG_SP <= REG_R; end
				1: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end
			   2: begin /* wait cycle */ end
			   3: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
			endcase
		end
		
//==============================================================================	
	23 /* LFS N */ : begin
		
			case (EXEC_COUNTER)	
				0: begin DIO_ADDR <= REG_I_PTR + 1; end
			   1: begin /* wait cycle */ end
				2: begin REG_R <= dio + REG_SP + 1; end
				3: begin DIO_ADDR <= REG_R; end
			   4: begin /* wait cycle */ end
				5: begin REG_R <= dio; end				
				
				6: begin REG_I_PTR <= REG_I_PTR + 2; DIO_ADDR <= REG_I_PTR + 2; end
			   7: begin /* wait cycle */ end
			   8: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
			endcase
		end		
//==============================================================================	
	24 /* STS N */ : begin
		
			case (EXEC_COUNTER)	
				0: begin DIO_ADDR <= REG_I_PTR + 1; end
			   1: begin /* wait cycle */ end
				2: begin REG_B <= dio + REG_SP + 1; end
				3: begin DIO_ADDR <= REG_B; MEM_OUT <= REG_R; WR_MEM <= 1; end
				
				4: begin REG_I_PTR <= REG_I_PTR + 2; DIO_ADDR <= REG_I_PTR + 2; WR_MEM <= 0; end
			   5: begin /* wait cycle */ end
			   6: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
			endcase
		end		
//==============================================================================	
	25 /* PUSH */ : begin
		
			case (EXEC_COUNTER)
				0: begin DIO_ADDR <= REG_SP; MEM_OUT <= REG_R; WR_MEM <= 1; REG_SP <= REG_SP - 1; end
				1: begin /* wait cycle */  WR_MEM <= 0; end
				2: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end
			   3: begin /* wait cycle */ end
			   4: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
			endcase
		end				
//==============================================================================	
	26 /* POP */ : begin
		
			case (EXEC_COUNTER)
				0: begin REG_SP <= REG_SP + 1; end
			
				1: begin DIO_ADDR <= REG_SP; end
			   2: begin /* wait cycle */ end
			   3: begin REG_R <= dio; end

				4: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end
			   5: begin /* wait cycle */ end
			   6: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
			endcase
		end						

// AUX
//==============================================================================	
	27 /* LDR ADDR */ : begin
		
			case (EXEC_COUNTER)	
				
				0: begin DIO_ADDR <= REG_I_PTR + 1; end
			   1: begin /* wait cycle */ end
				2: begin DIO_ADDR <= dio; end
				3: begin /* wait cycle */ end
				4: begin REG_R    <= dio; end		
								
				5: begin REG_I_PTR <= REG_I_PTR + 2; DIO_ADDR <= REG_I_PTR + 2; end
			   6: begin /* wait cycle */ end
			   7: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
				
			endcase
		end
//==============================================================================	
	28 /* STR ADDR */ : begin
		
			case (EXEC_COUNTER)	
				0: begin DIO_ADDR <= REG_I_PTR + 1; end
			   1: begin /* wait cycle */ end
				2: begin
						DIO_ADDR <= dio;
						MEM_OUT  <= REG_R;
						WR_MEM   <= 1;	
					end
				
				3: begin /* wait cycle */ WR_MEM <= 0; end
				4: begin REG_I_PTR <= REG_I_PTR + 2; DIO_ADDR <= REG_I_PTR + 2; end
			   5: begin /* wait cycle */ end
			   6: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
			endcase
		end
//==============================================================================	
	29 /* RB */ : begin
		
			case (EXEC_COUNTER)	
				0: begin REG_R <= REG_B; end
				
				1: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end
			   2: begin /* wait cycle */ end
			   3: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
			endcase
		end
//==============================================================================	
	30 /* BR */ : begin
		
			case (EXEC_COUNTER)	
				0: begin REG_B <= REG_R; end
				
				1: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end
			   2: begin /* wait cycle */ end
			   3: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
			endcase
		end
//==============================================================================	
	31 /* LDRA ADDR */ : begin
		
			case (EXEC_COUNTER)	
				0: begin DIO_ADDR <= REG_I_PTR + 1; end
			   1: begin /* wait cycle */ end
				2: begin REG_R <= dio; end
				
				3: begin REG_I_PTR <= REG_I_PTR + 2; DIO_ADDR <= REG_I_PTR + 2; end
			   4: begin /* wait cycle */ end
			   5: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
			endcase
		end
//==============================================================================	
	32 /* LDBA */ : begin
		
			case (EXEC_COUNTER)	
				0: begin DIO_ADDR <= REG_B; end
			   1: begin /* wait cycle */ end
				2: begin REG_R <= dio; end
				
				3: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; end
			   4: begin /* wait cycle */ end
			   5: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
			endcase
		end		
//==============================================================================	
	33 /* STBA */ : begin
		
			case (EXEC_COUNTER)	
				0: begin
						DIO_ADDR <= REG_B;
						MEM_OUT  <= REG_R;
						WR_MEM   <= 1;
					end
				
				1: begin REG_I_PTR <= REG_I_PTR + 1; DIO_ADDR <= REG_I_PTR + 1; WR_MEM <= 0; end
			   2: begin /* wait cycle */ end
			   3: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
			endcase
		end

// JMP		
//==============================================================================	
	34 /* JMP ADDR */ : begin
		
			case (EXEC_COUNTER)	
				0: begin
						DIO_ADDR  <= REG_I_PTR + 1;
					end
				1: begin /* wait cycle */ end
				2: begin REG_I_PTR <= dio; DIO_ADDR <= dio; end
				3: begin /* wait cycle */ end				
				4: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
			endcase
		end
//==============================================================================	
	35 /* JZ  ADDR */ : begin
				if (REG_R == 0) begin
					case (EXEC_COUNTER)	
						0: begin DIO_ADDR <= REG_I_PTR + 1; end
						1: begin /* wait cycle */ end
						2: begin REG_I_PTR <= dio; DIO_ADDR <= dio; end
						3: begin /* wait cycle */ end
						4: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
					endcase
				end else begin
					case (EXEC_COUNTER)
						0: begin DIO_ADDR <= REG_I_PTR + 2; REG_I_PTR <= REG_I_PTR + 2; end
						1: begin /* wait cycle */ end
						2: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
					endcase
				end
		end		
//==============================================================================	
	36 /* JNZ  ADDR */ : begin
		
				if (REG_R != 0) begin
					case (EXEC_COUNTER)	
						0: begin DIO_ADDR <= REG_I_PTR + 1; end
						1: begin /* wait cycle */ end
						2: begin REG_I_PTR <= dio; DIO_ADDR <= dio; end
						3: begin /* wait cycle */ end
						4: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
					endcase
				end else begin
					case (EXEC_COUNTER)
						0: begin DIO_ADDR <= REG_I_PTR + 2; REG_I_PTR <= REG_I_PTR + 2; end
						1: begin /* wait cycle */ end
						2: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
					endcase
				end
		end				
//==============================================================================	
	37 /* CALL ADDR */ : begin
		
			case (EXEC_COUNTER)	
				0: begin
						DIO_ADDR <= REG_SP;
						MEM_OUT  <= REG_I_PTR + 2;
						WR_MEM   <= 1;
						REG_SP   <= REG_SP - 1;
					end
				
				1: begin DIO_ADDR <= REG_I_PTR + 1; WR_MEM <= 0; end
			   2: begin /* wait cycle */ end
			   3: begin REG_I_PTR <= dio; DIO_ADDR <= dio; end
				4: begin /* wait cycle */ end
				5: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end
				
			endcase
		end				
//==============================================================================	
	38 /* CLS NUM_PARAMS */ : begin
		
			case (EXEC_COUNTER)	
			
				1: begin DIO_ADDR <= REG_I_PTR + 1; end
			   2: begin /* wait cycle */ end
			   3: begin REG_SP <= REG_SP + dio; end
				
				4: begin REG_I_PTR <= REG_I_PTR + 2; DIO_ADDR <= REG_I_PTR + 2; end
			   5: begin /* wait cycle */ end
			   6: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end				
			endcase
		end				
//==============================================================================	
	39 /* RET */ : begin
		
			case (EXEC_COUNTER)	
			
				1: begin REG_SP <= REG_SP + 1; end
				2: begin DIO_ADDR <= REG_SP; end
			   3: begin /* wait cycle */ end
				4: begin REG_I_PTR <= dio; DIO_ADDR <= dio; end
			   5: begin /* wait cycle */ end
			   6: begin REG_INSTRUCTION <= dio; EXEC_COUNTER <= 0; end				
			endcase
		end				
		
	endcase
	
end

endmodule