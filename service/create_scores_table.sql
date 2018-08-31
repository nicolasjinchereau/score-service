CREATE TABLE `scores` (
        `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
        `game` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL,
        `name` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL,
        `score` int(10) unsigned NOT NULL,
        `date` datetime NOT NULL,
        PRIMARY KEY (`id`),
        UNIQUE KEY `id` (`id`)
    ) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;