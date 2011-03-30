using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Windows.Navigation;
using System.Threading;
using System.Text;

namespace BrainfuckInterpreter
{
    public partial class MainPage : PhoneApplicationPage
    {
        private bool isRunning = false;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
            this.Loaded +=new RoutedEventHandler(MainPage_Loaded);
            updateUIRunStatus();
        }

        // Load data for the ViewModel Items
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            this.isRunning = true;
            updateUIRunStatus();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            this.isRunning = false;
            updateUIRunStatus();    
        }

        private void updateUIRunStatus()
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (this.isRunning)
                {
                    startExecution();
                }
                this.btnRun.IsEnabled = !this.isRunning;
                this.btnStop.IsEnabled = this.isRunning;
                this.txtSource.IsEnabled = !this.isRunning;
                this.txtIO.IsEnabled = this.isRunning;

            });
        }

        private int ip = 0;
        private int sx = 0;
        private const int MEMORY_SIZE = 30000;
        private byte[] memory = new byte[MEMORY_SIZE];
        private string sourceCode = string.Empty;
        private Stack<int> stack = new Stack<int>();
        private bool unicodeMode = false;

        private void processNextInstruction()
        {
            if (++ip == sourceCode.Length)
            {
                //ip = 0;
                // terminate immediately.
                this.isRunning = false;
                updateUIRunStatus();
                return;
            }

            char instruction = sourceCode[ip];
            switch (instruction)
            {
                case '>': // increment data pointer
                    if (++sx >= MEMORY_SIZE)
                        sx = 0;
                    break;

                case '<': // decrement data pointer
                    if (--sx < 0)
                        sx = MEMORY_SIZE - 1;
                    break;

                case '+': // increment value at pointer
                    memory[sx]++;
                    break;

                case '-': // decrement value at pointer
                    memory[sx]--;
                    break;

                case '.': // output character at pointer
                    if (unicodeMode)
                        // unicode mode active - push utf16-le straight to the buffer
                        putChar(Encoding.Unicode.GetString(memory, sx, 2));
                    else
                        // .net compact framework doesn't support ascii (wtf?).  so we need to translate it.
                        putChar(Encoding.Unicode.GetString(new byte[] { memory[sx], 0 }, 0, 2));

                    break;

                case ',': // input character at pointer, not implemented
                    // we should just terminate the program here
                    this.isRunning = false;
                    updateUIRunStatus();
                    putChar("\r\n\r\nInput opcode found - not supported!");
                    break;


                case '[': // start loop
                    if (memory[sx] == 0)
                    {
                        // find the matching ]
                        for (int count = 0; ; ip++)
                        {
                            if (sourceCode[ip] == '[') count++;
                            else if (sourceCode[ip] == ']') count--;
                            if (count == 0) break;
                        }
                    }
                    else
                    {
                        // push the stack
                        stack.Push(ip);
                    }
                    break;

                case ']': // end loop
                    // pop the stack
                    ip = stack.Pop() - 1;
                    break;

                case '!': // exit
                    this.isRunning = false;
                    updateUIRunStatus();
                    break;

                case '@': 
                    // set unicode mode based on whatever the value is being pointed at.
                    // If the memory value is non-zero, unicode support is turned on
                    this.unicodeMode = (memory[sx] >= 1);
                    break;

                default: // unknown
                    // do nothing.
                    break;

            }
        }

        private string output = string.Empty;

        private void putChar(string s)
        {
            output += s;
            Dispatcher.BeginInvoke(() =>
            {
                txtIO.Text = output;
                txtIO.UpdateLayout();
            });
        }

        
        private Thread executionThread;

        private void startExecution()
        {
            this.ip = 0;
            this.sx = 0;
            this.memory = new byte[MEMORY_SIZE];
            this.unicodeMode = false;
            Array.Clear(this.memory, 0, this.memory.Length);
            this.sourceCode = this.txtSource.Text;
            this.stack = new Stack<int>();
            executionThread = new Thread(this.executionLoop);
            executionThread.Start();
        }

        private void executionLoop()
        {
            while (isRunning)
            {
                //Thread.Sleep(100);
                processNextInstruction();
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            txtIO.Text = "";
            output = "";
        }

    }
}