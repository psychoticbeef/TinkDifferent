using System;
using System.Threading;
using Tinkerforge;

namespace TinkDifferent
{
	public class LCDisplayHelper
	{
		private Mutex mutex = new Mutex();
		private String[] display = new String[4];
		private DateTime[] delay = new DateTime[4];
		private int[] current_character_offset = new int[4];
		private bool[] scroll_direction = new bool[4];
				
		private BrickletLCD20x4 lcd;

		private int Initial_Scroll_Delay;
		public int initial_scroll_delay {
			get { return Initial_Scroll_Delay; }
			set { Initial_Scroll_Delay = value; }
		}
		
		private int Scroll_Delay_After_Each_Character;
		public int scroll_delay_after_each_character {
			get { return Scroll_Delay_After_Each_Character; }
			set { Scroll_Delay_After_Each_Character = value; }
		}
		
		private LCDisplayHelper() {
		}
		
		public LCDisplayHelper(BrickletLCD20x4 lcd) {
			this.lcd = lcd;
			Initial_Scroll_Delay = 4000;
			Scroll_Delay_After_Each_Character = 1000;
			Thread scroll_thread = new Thread(new ThreadStart(scroller));
			
			for (int i = 0; i < display.Length; i++) {
				display[i] = string.Empty;
				delay[i] = DateTime.Now;
				current_character_offset[i] = 0;
				scroll_direction[i] = false;
			}
			
			scroll_thread.Start();
		}
		
		public void setTextForLine(int line_number, string text) {
			mutex.WaitOne();
			display[line_number] = text;
			delay[line_number] = DateTime.Now;
			current_character_offset[line_number] = 0;
			scroll_direction[line_number] = false;
			mutex.ReleaseMutex();
		}
		
		public void scroller() {
			while(true) {
				mutex.WaitOne();
				for (byte i = 0; i < display.Length; i++) {
					if (display[i].Length <= 20) {
						continue;
					}
					
					bool changed = false;
					
					TimeSpan ts;
					
				// case 1: first character or last, i.e. we wait for initial_scroll_delay
					if (i == 3)
					if (current_character_offset[i] == 0 || current_character_offset[i] == display[i].Length - 20) {
						ts = DateTime.Now.Subtract(delay[i]);
						if (ts.Milliseconds + ts.Seconds * 1000 + ts.Minutes * 60 * 1000 >= Initial_Scroll_Delay) {
							scroll_direction[i] = !scroll_direction[i];
							changed = true;
						}
					} else {
				
				// case 2: we are somewhere in the middle of a string, so we wait for Scroll_Delay_After_Each_Character
						ts = DateTime.Now.Subtract(delay[i]);
						if (ts.Milliseconds + ts.Seconds * 1000 + ts.Minutes * 60 * 1000 >= Scroll_Delay_After_Each_Character ) {
							changed = true;
						}
					}
					
					if (changed) {
						if (scroll_direction[i]) {
							current_character_offset[i]++;
						} else {
							current_character_offset[i]--;
						}
						delay[i] = DateTime.Now;
						
						string result = display[i].Substring(current_character_offset[i], 20);
						lcd.WriteLine(i, 0, result);
#if DEBUG
						Console.WriteLine(result);
#endif
					}
					
				}
				mutex.ReleaseMutex();
				Thread.Sleep(Math.Min(Initial_Scroll_Delay, Scroll_Delay_After_Each_Character));
			}
		}

		
	}
}

