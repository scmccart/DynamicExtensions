using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DynamicExtensions;

namespace DwemthysArray
{
    class Program
    {
        static void Main(string[] args)
        {
            var Rabbit = new
            {
                Name = "Rabbit",
                Life = 10D,
                Strength = 2D,
                Charisma = 44D,
                Weapon = 4D,
                bombs = 3
            }.Transmute().MixIn(new CreatureMethods()).MixIn(new RabbitMethods()) as IRabbit;

            var IndustrialRaverMonkey = new
            {
                Name = "IndustrialRaverMonkey",
                Life = 46D,
                Strength = 35D,
                Charisma = 91D,
                Weapon = 2D
            }.Transmute().MixIn(new CreatureMethods());

            var DwarvenAngel = new
            {
                Name = "DwarvenAngel",
                Life = 540D,
                Strength = 6D,
                Charisma = 144D,
                Weapon = 50D
            }.Transmute().MixIn(new CreatureMethods());

            var AssistantViceTentacleAndOmbudsman = new
            {
                Name = "AssistantViceTentacleAndOmbudsman",
                Life = 320D,
                Strength = 6D,
                Charisma = 144D,
                Weapon = 50D
            }.Transmute().MixIn(new CreatureMethods());

            var TeethDeer = new
            {
                Name = "TeethDeer",
                Life = 655D,
                Strength = 192D,
                Charisma = 19D,
                Weapon = 109D
            }.Transmute().MixIn(new CreatureMethods());

            var IntrepidDecomposedCyclist = new
            {
                Name = "IntrepidDecomposedCyclist",
                Life = 901D,
                Strength = 560D,
                Charisma = 422D,
                Weapon = 105D
            }.Transmute().MixIn(new CreatureMethods());

            var Dragon = new
            {
                Name = "Dragon",
                Life = 1340D,
                Strength = 451D,
                Charisma = 1020D,
                Weapon = 939D
            }.Transmute().MixIn(new CreatureMethods());

            var dwarr = new List<object>()
            {
                IndustrialRaverMonkey,
                DwarvenAngel,
                AssistantViceTentacleAndOmbudsman,
                TeethDeer,
                IntrepidDecomposedCyclist,
                Dragon
            }.Transmute().MixIn(new DwemthysArrayMethods());

            Rabbit.Lettuce(dwarr as ICreature);

            Console.Write("Done");
            Console.ReadLine();
        }

        interface ICreature
        {
            string Name { get; set; }
            double Life { get; set; }
            double Strength { get; set; }
            double Charisma { get; set; }
            double Weapon { get; set; }
            void Hit(double damage);
            void Fight(ICreature enemy, double weapon);
        }

        interface IBomber : ICreature
        {
            int Bombs { get; set; }
        }

        interface IRabbit
        {
            void Boomerang(ICreature enemy);

            void Sword(ICreature enemy);

            void Lettuce(ICreature enemy);

            void Bomb(ICreature enemy);
        }

        class CreatureMethods
        {
            Random r = new Random();

            public void Hit(ICreature self, double damage)
            {
                var p_up = r.NextDouble() * self.Charisma;

                if (p_up % 9 == 7)
                {
                    self.Life += p_up / 4;
                    Console.WriteLine("{0} magick powers up {1}!", self.Name, p_up);
                }

                self.Life -= damage;
                if (self.Life <= 0)
                    Console.WriteLine("[{0} has died.]", self.Name);
            }

            public void Fight(ICreature self, ICreature enemy, double weapon)
            {
                if (self.Life <= 0)
                    Console.WriteLine("[{0} is too dead to fight!]", self.Name);
                else
                {
                    var your_hit = r.NextDouble() * (self.Strength + self.Weapon);
                    Console.WriteLine("[You hit with {0} points of damage!]", your_hit);
                    enemy.Hit(your_hit);

                    if (enemy.Life > 0)
                    {
                        var enemy_hit = r.NextDouble() * (enemy.Strength + enemy.Weapon);
                        Console.WriteLine("[Your enemy hit with {0} points of damage!]", enemy_hit);
                        self.Hit(enemy_hit);
                    }
                }
            }
        }

        class RabbitMethods
        {
            Random r = new Random();

            public void Boomerang(ICreature self, ICreature enemy)
            {
                self.Fight(enemy, 13);
            }

            public void Sword(ICreature self, ICreature enemy)
            {
                self.Fight(enemy, r.NextDouble() * (4 + (Math.Pow(enemy.Life % 10, 2))));
            }

            public void Lettuce(ICreature self, ICreature enemy)
            {
                var lettuce = r.NextDouble() * self.Charisma;
                Console.WriteLine("[Healthy lettuce gives you {0} life points!!]", lettuce);
                self.Life += lettuce;
                self.Fight(enemy, 0);
            }

            public void Bomb(IBomber self, ICreature enemy)
            {
                if (self.Bombs == 0)
                    Console.WriteLine("[UHN!! You're out of bombs!!]");
                else
                {
                    self.Bombs--;
                    self.Fight(enemy, 86);
                }
            }
        }

        class DwemthysArrayMethods
        {
            public object MethodMissing(IList<ICreature> self, string methodName, object[] args)
            {
                var first = self.First();

                var ret = first.Send(methodName, args);

                if (first.Life <= 0)
                {
                    self.RemoveAt(0);
                    if (self.Count() == 0)
                        Console.WriteLine("[Whoa.  You decimated Dwemthy's Array!]");
                    else
                        Console.WriteLine("[Get ready. {0} has emerged.]", self.First().Name);
                }

                return ret ?? 0;
            }
        }
    }
}
