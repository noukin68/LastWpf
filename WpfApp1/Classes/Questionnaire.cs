using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Classes
{
    public class Questionnaire
    {
        public List<Question> Questions { get; }

        public Questionnaire()
        {
            Questions = new List<Question>
        {
            new Question("В каком году была Куликовская битва?", new List<string> { "1380", "1812", "1917", "1945" }, 0),
            new Question("Какое вещество обеспечивает зеленую окраску растений?", new List<string> { "Железо", "Кальций", "Магний", "Фосфор" }, 2),
            new Question("Какой химический элемент обозначается символом 'O'?", new List<string> { "Оксиген", "Озон", "Огонь", "Олово" }, 0),
            new Question("Кто написал произведение 'Война и мир'?", new List<string> { "Фёдор Достоевский", "Лев Толстой", "Иван Тургенев", "Антон Чехов" }, 1),
            new Question("Что измеряется в герцах?", new List<string> { "Сила тока", "Частота", "Сопротивление", "Напряжение" }, 1),
            new Question("Какое из этих животных является хищником?", new List<string> { "Кролик", "Олень", "Лев", "Корова" }, 2),
            new Question("Какая планета Солнечной системы известна своим кольцом?", new List<string> { "Марс", "Сатурн", "Юпитер", "Венера" }, 1),
            new Question("Кто написал пьесу 'Ромео и Джульетта'?", new List<string> { "Вильям Шекспир", "Оскар Уайльд", "Генрих Ибсен", "Антон Чехов" }, 0),
            new Question("Какое событие начало Первую мировую войну?", new List<string> { "Взрыв атомной бомбы", "Подписание Версальского договора", "Убийство Архидука Франца Фердинанда", "Завоевание Константинополя" }, 2),
            new Question("Как называется самая большая планета в Солнечной системе?", new List<string> { "Марс", "Сатурн", "Юпитер", "Венера" }, 2)
        };
        }
    }
}
