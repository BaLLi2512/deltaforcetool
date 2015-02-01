namespace PokehBuddy
{
    public partial class PokehBuddy
    {
        private static string ConvertFromOldFile(string logic)
        {



            //Chinese, japanese, russian and other weird character users should delete this line :
            return logic.Replace('·', '@').Replace('*', '@').Replace('þ', '$');
            //(you will lose backwards compatibility with old logic files)

        }

    }
}