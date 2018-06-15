﻿using System;
using System.Data.SqlClient;

namespace Sikiro.DapperLambdaExtension.MsSql.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            var con = new SqlConnection(
                " Data Source=192.168.13.46;Initial Catalog=SkyChen;Persist Security Info=True;User ID=sa;Password=123456789");

            var db = new DataBase(con);

            var deleteResult = db.Set<SysUser>().Where(a => a.UserName == "chengong").Delete() > 0;

            Console.WriteLine("删除数{0}", deleteResult);

            var insertResult = db.Set<SysUser>().Insert(new SysUser
            {
                CreateDatetime = DateTime.Now,
                Email = "287245177@qq.com",
                Mobile = "18988561111",
                RealName = "陈珙",
                SysUserid = Guid.NewGuid().ToString("N"),
                UserName = "chengong",
                UserStatus = 1,
                UserType = 1,
                Password = "asdasdad"
            });
            Console.WriteLine("添加数{0}", insertResult);

            var countResult = db.Set<SysUser>().Where(a => a.Email == "287245177@qq.com").Count();
            Console.WriteLine("查询个数{0}", insertResult);

            var getResult = db.Set<SysUser>().Where(a => a.Email == "287245177@qq.com").Get();

            var listResult = db.Set<SysUser>().OrderBy(a => a.CreateDatetime).Select(a => a.Email).ToList();

            var listResult2 = db.Set<SysUser>().OrderBy(a => a.CreateDatetime).Top(2).Select(a => a.Email).ToList();

            var updateResult = db.Set<SysUser>().Where(a => a.Email == "287245177@qq.com")
                .Update(a => new SysUser { UserStatus = 1 });

            getResult.Email = "287245145666@qq.com";
            var updateResult2 = db.Set<SysUser>().Where(a => a.Email == "287245177@qq.com").Update(getResult);

            var updateResult3 = db.Set<SysUser>().Where(a => a.Email == "287245177@qq.com").OrderBy(b => b.Email)
                .Top(10).Select(a => a.Email).ToList();

            var updateResult4 = db.Set<SysUser>().Sum(a => a.UserStatus);

            var updateResult5 = db.Set<SysUser>().Where(a => a.Email == "456465asd@qq.com")
                .Select(a => new SysUser { Email = a.Email, Mobile = a.Mobile, Password = a.Password })
                .UpdateSelect(a => new SysUser { Email = "456465asd@qq.com" });

            var updateResult6 = db.Set<SysUser>().Where(a => a.Email == "456465asd@qq.com")
                .OrderBy(a => a.CreateDatetime)
                .Select(a => new SysUser { Email = a.Email, Mobile = a.Mobile, Password = a.Password }).PageList(1, 10);

            db.Dispose();
        }
    }
}