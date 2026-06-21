module UART (
    input  wire clk_in,
	 	 	 
	 input  wire  in_RXD,
	 output wire out_TXD,

	 input  wire [7:0]  in_data,
	 output wire [7:0] out_data,
		 
	 output wire out_tx_busy,
	 output wire out_rx_ready,
	 
	 input  wire  in_wr,
	 
	 input  wire  in_rd
);

// === === ===

localparam integer IN_FREQ = 100000000;
localparam integer SIG_FREQ = 57600;
localparam integer FREQ_DIV = IN_FREQ / SIG_FREQ;

// === === ===

reg out_bit = 1;
assign out_TXD = out_bit;

reg [7:0] tx_data;
reg [7:0] rx_data;

reg writing = 0;
integer tx_freq_counter = FREQ_DIV;
integer tx_bit_counter  = 0;

reg in_bit      = 1;
reg in_bit_prev = 1;

reg reading = 0;
integer rx_freq_counter = FREQ_DIV/2;
integer rx_bit_counter  = 0;
reg rd_ready = 0;


assign out_tx_busy  = writing;
assign out_rx_ready = rd_ready;
	 
assign out_data = rx_data;

// === === ===

//integer dbg_counter = 0;
//integer dbg_state = 0;

always @(posedge clk_in) begin

//	dbg_counter <= dbg_counter + 1;
//	if (dbg_counter >= IN_FREQ) begin
//		dbg_counter <= 0;
//		
//		dbg_state <= dbg_state + 1;
//		if (dbg_state >= 2) begin dbg_state <= 0; end
//		case (dbg_state)
//            0 : begin tx_data <= "A"; end				
//				1 : begin tx_data <= "B"; end
//				2 : begin tx_data <= "C"; end
//		endcase
//		
//		writing <= 1;
//		
//	end

//	if (rd_ready) begin
//		rd_ready <= 0;
//		
//		writing <= 1;
//		tx_data <= rx_data;
//		
//	end

	if (in_wr) begin
		writing <= 1;
		tx_data <= in_data;
	end

	if (in_rd) begin
		rd_ready <= 0;
	end
	
	// TX

	if (writing) begin
		
		tx_freq_counter <= tx_freq_counter + 1;
		if (tx_freq_counter >= FREQ_DIV) begin
			tx_freq_counter <= 0;
			
			tx_bit_counter <= tx_bit_counter + 1;
			
			case (tx_bit_counter)
            0 : begin out_bit <= 0; end
				
				1 : begin out_bit <= tx_data[0]; end
				2 : begin out_bit <= tx_data[1]; end
				3 : begin out_bit <= tx_data[2]; end
				4 : begin out_bit <= tx_data[3]; end
				5 : begin out_bit <= tx_data[4]; end
				6 : begin out_bit <= tx_data[5]; end
				7 : begin out_bit <= tx_data[6]; end
				8 : begin out_bit <= tx_data[7]; end
				
				9 : begin out_bit <= 1; end
				
				10: begin
				
					writing         <= 0;
					tx_bit_counter  <= 0;
					tx_freq_counter <= 0;
					
				end

			endcase
		
		end
		
	end

	// RX
	
	in_bit      <= in_RXD;
	in_bit_prev <= in_bit;

	if (in_bit_prev && !in_bit && !reading) begin
		reading <= 1;
		rx_bit_counter  <= 0;
		rx_freq_counter <= (FREQ_DIV/2);
	end
	
	if (reading) begin
		
		rx_freq_counter <= rx_freq_counter + 1;
		if (rx_freq_counter >= FREQ_DIV) begin
			rx_freq_counter <= 0;
			
			rx_bit_counter <= rx_bit_counter + 1;
			
			case (rx_bit_counter)
				
				0 : begin ; end
								
				1 : begin rx_data[0] <= in_bit; end
				2 : begin rx_data[1] <= in_bit; end
				3 : begin rx_data[2] <= in_bit; end
				4 : begin rx_data[3] <= in_bit; end
				5 : begin rx_data[4] <= in_bit; end
				6 : begin rx_data[5] <= in_bit; end
				7 : begin rx_data[6] <= in_bit; end
				8 : begin
				
					       rx_data[7] <= in_bit;
				
					reading         <= 0;
					rd_ready        <= 1;
					
				end

			endcase
		
		end
		
	end
	
end

endmodule