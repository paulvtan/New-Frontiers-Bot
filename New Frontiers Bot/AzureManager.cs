using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.MobileServices;
using New_Frontiers_Bot.DataModels;
using System.Threading.Tasks;

namespace New_Frontiers_Bot
{
    public class AzureManager
    {
        private static AzureManager instance;
        private MobileServiceClient client;
        private IMobileServiceTable<ShoppingList> shoppingListTable;

        //Constructor
        private AzureManager()
        {
            this.client = new MobileServiceClient("http://blankwebapptohosteasytable.azurewebsites.net/");
            this.shoppingListTable = this.client.GetTable<ShoppingList>(); //This grab the table from the web app.
        }

        //Getter for the AzureClient
        public MobileServiceClient AzureClient
        {
            get { return client; }
        }

        public static AzureManager AzureManagerInstace
        {
            get
            {
                if (instance == null)
                {
                    instance = new AzureManager();
                }

                return instance;
            }
        }

        public async Task<List<ShoppingList>> GetShoppingList()
        {
            return await this.shoppingListTable.ToListAsync();
        }

        public async Task AddShoppingList(ShoppingList shoppingList)
        {
            await this.shoppingListTable.InsertAsync(shoppingList);
        }



    }
}