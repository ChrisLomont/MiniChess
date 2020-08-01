// Chess program
// Chris Lomont 2020
// run the chess program
class Program
{
    static void Main(string[] args)
    {
        //var state = new State();
        //MoveGen.Perft(state,5,true);
        //return;

        Testing.PerfTests();
        return;


        var chess = new Chess();
        chess.Interactive();
    }
}
