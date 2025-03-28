public class DataManager
{
    private static DataManager instance;

    private DataManager()
    {
    }

    public static DataManager GetInstance()
    {
        if (DataManager.instance == null)
        {
            DataManager.instance = new DataManager();
        }
        return DataManager.instance;
    }
}