﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using Npgsql;
namespace WindowsService1
{
    public partial class Service1 : ServiceBase
    {
        Timer timer;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            if (timer == null)
            {
                timer = new Timer();
            }
            timer.Interval = 120000;//86400000
            timer.Elapsed += Timer_Elapsed;
            timer.Enabled = true;
            timer.Start();
            
        }

        protected override void OnStop()
        {
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
            Task.Run(() => Update_PriceAsync());
            
            timer.Start();
        }

       
            
            public async void Update_PriceAsync()
            {
                NpgsqlConnection connectionString = new NpgsqlConnection("server=localHost;port=5432;Database=Solution1;user ID=postgres;password=1234;");
                DateTime time = DateTime.Now.AddDays(1);
                string format = "yyy-MM-dd";
                string tarih = time.ToString(format);
            //Console.WriteLine(tarih);

            string url = $"https://seffaflik.epias.com.tr/transparency/service/market/mcp-average?startDate={tarih}&endDate={tarih}&period=DAILY";
                //Console.WriteLine(url);
                try
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        HttpResponseMessage response = await httpClient.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            string fiyat;
                            DateTime day;
                            string content = await response.Content.ReadAsStringAsync();
                            dynamic data = JsonConvert.DeserializeObject(content);

                            foreach (var item in data.body.dayAheadMCPList)
                            {
                            fiyat = Convert.ToString(item.averageMcp);
                            day = Convert.ToDateTime(item.date);
                            File.AppendAllText("log.txt", fiyat);
                            File.AppendAllText("log.txt", day.ToString());

                            try
                            {
                                connectionString.Open();
                                File.AppendAllText("log.txt", "bağlantı başarılı");
                            }
                            catch (Exception)
                            {

                                throw;
                            }
                            try
                            {
                                string queryStr = "Update \"Fiyatlar\" set \"Fiyat\" = @fiyat , \"Tarih\"=@tarih";
                                Console.WriteLine(fiyat + " " + day);
                                NpgsqlCommand cmd = new NpgsqlCommand(queryStr, connectionString);
                                cmd.Parameters.AddWithValue("@fiyat", fiyat);
                                cmd.Parameters.AddWithValue("@tarih", day);
                                cmd.ExecuteNonQuery();
                                cmd.Dispose();
                                File.AppendAllText("log.txt", "ekleme başarılı");
                            }
                            catch (Exception)
                            {

                                throw;
                            }
                            try
                            {
                                connectionString.Close();
                                File.AppendAllText("log.txt", "çıkış yapıldı");
                            }
                            catch (Exception)
                            {

                                throw;
                            }
                        }

                        }
                        else
                        {
                            Console.WriteLine($"Veri alınamadı. Hata kodu: {response.StatusCode}");
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hata oluştu: {ex.Message}");
                }
            

            
        }

    }
}
