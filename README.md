Проостенькая утилитка на Рослине для добавления ```readonly``` кейворда методам в не ридонли структурах. Находит не все валидные места, 
но false positive случаев почти нет. Пока что не обрабатывает случаи, когда в методе меняется какой-нибудь внутренний лист (компилятор все еще позовляет помечать такие методы 
как ридонли)
