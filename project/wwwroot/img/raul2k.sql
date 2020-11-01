-- phpMyAdmin SQL Dump
-- version 5.0.2
-- https://www.phpmyadmin.net/
--
-- Gazdă: 127.0.0.1
-- Timp de generare: mai 18, 2020 la 08:07 PM
-- Versiune server: 10.4.11-MariaDB
-- Versiune PHP: 7.2.29

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Bază de date: `raul2k`
--

-- --------------------------------------------------------

--
-- Structură tabel pentru tabel `categories`
--

CREATE TABLE `categories` (
  `name` text NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Eliminarea datelor din tabel `categories`
--

INSERT INTO `categories` (`name`) VALUES
('Silver'),
('Gold Nova'),
('Master Guardian'),
('Legendary Eagle'),
('Supreme'),
('Global Elite');

-- --------------------------------------------------------

--
-- Structură tabel pentru tabel `csgoaccounts`
--

CREATE TABLE `csgoaccounts` (
  `id` int(11) NOT NULL,
  `seller` text NOT NULL,
  `username` text NOT NULL,
  `password` text NOT NULL,
  `rank` int(11) NOT NULL DEFAULT 0,
  `price` decimal(10,2) NOT NULL,
  `date` datetime NOT NULL DEFAULT current_timestamp(),
  `status` int(11) NOT NULL DEFAULT 0,
  `description` text NOT NULL,
  `prime` tinyint(1) NOT NULL,
  `email` text NOT NULL,
  `emailPassword` text NOT NULL,
  `hours` int(11) NOT NULL,
  `wins` int(11) NOT NULL,
  `image` text NOT NULL,
  `steamid64` bigint(20) NOT NULL,
  `buyer` text NOT NULL,
  `buyerid` int(11) NOT NULL DEFAULT -1,
  `sellerid` int(11) NOT NULL,
  `ticketId` int(11) NOT NULL DEFAULT -1,
  `disputed` tinyint(1) NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Eliminarea datelor din tabel `csgoaccounts`
--

INSERT INTO `csgoaccounts` (`id`, `seller`, `username`, `password`, `rank`, `price`, `date`, `status`, `description`, `prime`, `email`, `emailPassword`, `hours`, `wins`, `image`, `steamid64`, `buyer`, `buyerid`, `sellerid`, `ticketId`, `disputed`) VALUES
(2, 'denis', 'picolino209', 'undefined', 0, '15.00', '2020-05-14 13:40:11', 2, '', 0, 'raulsa252@gmail.com', '', 0, 0, 'https://i.ytimg.com/vi/e3Gcp0rzQKI/maxresdefault.jpg', 76561198825425038, 'AlexConstangeles', 3, 1, 3, 0),
(3, 'lighty', 'lemeshyiri33', '9H5VV-M7D6H', 17, '45.00', '2020-05-18 16:14:49', 1, 'nu cumparati', 0, 'teodscarlat32@gmail.com', 'teodscarlat32@gmail.com', 123, 657, 'https://i.imgur.com/5bpFE7x.png', 76561198810465177, 'denis', 1, 14, 4, 0);

-- --------------------------------------------------------

--
-- Structură tabel pentru tabel `rates`
--

CREATE TABLE `rates` (
  `id` int(11) NOT NULL,
  `ratedId` int(11) NOT NULL,
  `raterId` int(11) NOT NULL,
  `csgoId` int(11) NOT NULL,
  `date` date NOT NULL DEFAULT current_timestamp(),
  `message` text NOT NULL,
  `rate` tinyint(1) NOT NULL,
  `ratedUsername` text NOT NULL,
  `raterUsername` text NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Eliminarea datelor din tabel `rates`
--

INSERT INTO `rates` (`id`, `ratedId`, `raterId`, `csgoId`, `date`, `message`, `rate`, `ratedUsername`, `raterUsername`) VALUES
(1, 1, 3, 2, '2020-05-14', 'da contu fututi mm', 0, 'denis', 'AlexConstangeles'),
(2, 14, 1, 3, '2020-05-18', 'e tepar', 0, 'lighty', 'denis');

-- --------------------------------------------------------

--
-- Structură tabel pentru tabel `tickets`
--

CREATE TABLE `tickets` (
  `id` int(11) NOT NULL,
  `csgoId` int(11) NOT NULL,
  `creationTime` datetime NOT NULL DEFAULT current_timestamp(),
  `against` text NOT NULL,
  `againstId` int(11) NOT NULL,
  `fromUser` text NOT NULL,
  `fromUserId` int(11) NOT NULL,
  `closed` tinyint(1) NOT NULL DEFAULT 0,
  `type` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Eliminarea datelor din tabel `tickets`
--

INSERT INTO `tickets` (`id`, `csgoId`, `creationTime`, `against`, `againstId`, `fromUser`, `fromUserId`, `closed`, `type`) VALUES
(3, 2, '2020-05-14 22:31:15', 'denis', 1, 'AlexConstangeles', 3, 0, 1),
(4, 3, '2020-05-18 16:56:35', 'lighty', 14, 'denis', 1, 1, 1);

-- --------------------------------------------------------

--
-- Structură tabel pentru tabel `ticketsdata`
--

CREATE TABLE `ticketsdata` (
  `id` int(11) NOT NULL,
  `ticketId` int(11) NOT NULL,
  `fromId` int(11) NOT NULL,
  `date` datetime NOT NULL DEFAULT current_timestamp(),
  `fromUsername` text NOT NULL,
  `message` text NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Eliminarea datelor din tabel `ticketsdata`
--

INSERT INTO `ticketsdata` (`id`, `ticketId`, `fromId`, `date`, `fromUsername`, `message`) VALUES
(12, 3, 3, '2020-05-14 22:31:34', 'AlexConstangeles', 'sa ti bag muie ca mi ai dat scam fututi mm\n'),
(13, 3, 3, '2020-05-14 22:31:59', 'AlexConstangeles', 'dc mi ai dat scam ma dami contu'),
(14, 3, 1, '2020-05-14 22:32:07', 'denis', 'ce ai baiete ca nu ti-am dat scam'),
(15, 3, 1, '2020-05-14 22:32:44', 'denis', 'dfdfasdfsadfsafs'),
(16, 3, 3, '2020-05-14 22:33:52', 'AlexConstangeles', 'scamerule sa ti bag muie'),
(17, 3, 2, '2020-05-14 22:33:58', 'raul2k', 'fsdfdsf'),
(18, 3, 1, '2020-05-14 22:34:23', 'denis', 'sadfsafasf'),
(19, 3, 1, '2020-05-14 22:34:31', 'denis', 'adsffass'),
(20, 3, 3, '2020-05-14 22:34:39', 'AlexConstangeles', 'oo raul3k sa ma sugi'),
(21, 3, 1, '2020-05-14 22:34:54', 'denis', 'dsadadsa'),
(22, 3, 1, '2020-05-14 22:34:57', 'denis', 'gfdds'),
(23, 3, 2, '2020-05-14 22:35:09', 'raul2k', 'sadfafsaf'),
(24, 3, 3, '2020-05-14 22:35:32', 'AlexConstangeles', 'sa mi bag pula in site ul tau ca imi dai teapa futuvan gat'),
(25, 3, 1, '2020-05-14 22:35:36', 'denis', 'sadafads'),
(26, 3, 2, '2020-05-14 22:38:53', 'raul2k', 'asdadsd'),
(27, 3, 2, '2020-05-14 22:39:30', 'raul2k', 'adsdaads'),
(28, 3, 2, '2020-05-14 22:39:37', 'raul2k', 'asdads'),
(29, 3, 1, '2020-05-14 22:41:40', 'denis', 'fsadfdf'),
(30, 3, 1, '2020-05-14 22:42:06', 'denis', 'adffafadsf'),
(31, 3, 2, '2020-05-14 22:42:30', 'raul2k', 'sdadad'),
(32, 3, 1, '2020-05-14 22:43:11', 'denis', 'fdsfsfsf'),
(33, 3, 2, '2020-05-14 22:45:04', 'raul2k', 'dfsafdf'),
(34, 3, 2, '2020-05-14 22:47:37', 'raul2k', 'asdasad'),
(35, 3, 1, '2020-05-14 22:47:51', 'denis', 'fdfsgfs'),
(36, 3, 2, '2020-05-14 22:49:14', 'raul2k', 'dsaddsda'),
(37, 3, 1, '2020-05-14 22:49:22', 'denis', 'sdfaffds'),
(38, 3, 1, '2020-05-14 22:49:34', 'denis', 'dfsfdfs'),
(39, 3, 1, '2020-05-14 22:50:01', 'denis', 'fdsgdf'),
(40, 3, 2, '2020-05-14 22:50:32', 'raul2k', 'asdadsada'),
(41, 3, 1, '2020-05-14 22:50:51', 'denis', 'fffff'),
(42, 3, 2, '2020-05-14 22:51:28', 'raul2k', 'asasd'),
(43, 3, 1, '2020-05-14 22:51:34', 'denis', 'gfsfg'),
(44, 3, 1, '2020-05-14 22:51:40', 'denis', 'dggdfd'),
(45, 3, 2, '2020-05-14 22:54:19', 'raul2k', 'asdasd'),
(46, 3, 2, '2020-05-14 22:54:23', 'raul2k', 'asdad'),
(47, 3, 2, '2020-05-14 22:54:30', 'raul2k', 'safddfs'),
(48, 3, 2, '2020-05-14 22:55:06', 'raul2k', 'asdadsd'),
(49, 3, 1, '2020-05-14 22:55:26', 'denis', 'sadasdad'),
(50, 3, 2, '2020-05-14 22:55:54', 'raul2k', 'sdadsa'),
(51, 3, 2, '2020-05-14 22:56:07', 'raul2k', 'areaar'),
(52, 3, 1, '2020-05-14 22:58:02', 'denis', 'asdasda'),
(53, 3, 1, '2020-05-14 22:58:44', 'denis', 'asdad'),
(54, 3, 1, '2020-05-14 22:59:02', 'denis', 'asdadas'),
(55, 3, 1, '2020-05-14 23:00:22', 'denis', 'dsada'),
(56, 3, 1, '2020-05-14 23:00:54', 'denis', 'asdasdkjd'),
(57, 3, 1, '2020-05-14 23:01:01', 'denis', 'asddasadsads'),
(58, 3, 2, '2020-05-14 23:01:17', 'raul2k', 'ce vrei ma?'),
(59, 4, 1, '2020-05-18 16:56:47', 'denis', 'muie lighty');

-- --------------------------------------------------------

--
-- Structură tabel pentru tabel `transactions`
--

CREATE TABLE `transactions` (
  `id` int(11) NOT NULL,
  `userId` int(11) NOT NULL,
  `username` text NOT NULL,
  `email` text NOT NULL,
  `amount` decimal(10,2) NOT NULL,
  `type` int(11) NOT NULL,
  `date` text DEFAULT NULL,
  `status` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Eliminarea datelor din tabel `transactions`
--

INSERT INTO `transactions` (`id`, `userId`, `username`, `email`, `amount`, `type`, `date`, `status`) VALUES
(1, 2, 'denis', 'picoloware-buyer@yahoo.com', '3.00', 0, '2020-04-29', 0),
(2, 2, 'denis', 'picoloware-buyer@yahoo.com', '15000.00', 0, '2020-04-29', 0),
(3, 2, 'denis', 'picoloware-buyer@yahoo.com', '5.00', 0, '2020-04-29', 0),
(4, 2, 'denis', 'picoloware-buyer@yahoo.com', '2.00', 0, '4/29/2020 2:22:00 AM', 0),
(5, 2, 'denis', 'picoloware-buyer@yahoo.com', '1555.00', 0, '4/29/2020 10:51:29 PM', 0),
(6, 4, 'mDenis16', 'picoloware-buyer@yahoo.com', '150.00', 0, '5/2/2020 12:51:53 AM', 0),
(7, 4, 'mDenis16', 'picoloware-buyer@yahoo.com', '5.00', 0, '5/7/2020 11:46:33 AM', 0),
(8, 1, '', 'asd@sa.ro', '15.00', 2, '5/14/2020 1:45:33 PM', 0),
(9, 1, '', 'asd@sa.ro', '13.80', 3, '5/14/2020 1:45:33 PM', 0),
(10, 3, '', 'picoloware-buyer@yahoo.com', '50.00', 0, '5/14/2020 10:23:21 PM', 0),
(11, 3, '', 'asd@sa.ro', '15.00', 2, '5/14/2020 10:27:10 PM', 0),
(12, 1, '', 'jetssel942@gmail.com', '13.80', 3, '5/14/2020 10:27:10 PM', 0),
(13, 3, '', 'picoloware-buyer@yahoo.com', '1000.00', 0, '5/14/2020 10:36:21 PM', 0),
(14, 3, '', 'picoloware-buyer@yahoo.com', '10000.00', 0, '5/14/2020 10:37:40 PM', 0),
(15, 3, '', 'picoloware-buyer@yahoo.com', '10000.00', 0, '5/14/2020 10:38:14 PM', 0),
(16, 3, '', 'picoloware-buyer@yahoo.com', '10000.00', 0, '5/14/2020 10:39:14 PM', 0),
(17, 3, '', 'picoloware-buyer@yahoo.com', '10000.00', 0, '5/14/2020 10:48:55 PM', 0),
(18, 3, '', 'picoloware-buyer@yahoo.com', '10000.00', 0, '5/14/2020 10:59:55 PM', 0),
(19, 3, '', 'picoloware-buyer@yahoo.com', '1000.00', 0, '5/15/2020 12:07:48 AM', 0),
(20, 3, '', 'picoloware-buyer@yahoo.com', '50000.00', 0, '5/15/2020 12:08:45 AM', 0),
(21, 1, '', 'teodscarlat@gmail.com', '45.00', 2, '5/18/2020 4:15:38 PM', 0),
(22, 14, '', 'asd', '41.40', 3, '5/18/2020 4:15:38 PM', 0),
(23, 1, '', 'teodscarlat@gmail.com', '45.00', 2, '5/18/2020 4:17:19 PM', 0),
(24, 14, '', 'asd', '41.40', 3, '5/18/2020 4:17:19 PM', 0),
(25, 1, '', 'teodscarlat@gmail.com', '45.00', 2, '5/18/2020 4:20:24 PM', 0),
(26, 14, '', 'asd', '41.40', 3, '5/18/2020 4:20:24 PM', 0),
(27, 1, '', 'teodscarlat@gmail.com', '45.00', 2, '5/18/2020 4:22:45 PM', 0),
(28, 14, '', 'asd', '41.40', 3, '5/18/2020 4:22:45 PM', 0),
(29, 1, '', 'teodscarlat@gmail.com', '45.00', 2, '5/18/2020 4:23:32 PM', 0),
(30, 14, '', 'asd', '41.40', 3, '5/18/2020 4:23:32 PM', 0);

-- --------------------------------------------------------

--
-- Structură tabel pentru tabel `users`
--

CREATE TABLE `users` (
  `id` int(11) NOT NULL,
  `username` text NOT NULL,
  `password` text NOT NULL,
  `cookie` text NOT NULL,
  `balance` decimal(10,2) NOT NULL DEFAULT 0.00,
  `admin` tinyint(1) NOT NULL DEFAULT 0,
  `boughtAccounts` int(11) NOT NULL DEFAULT 0,
  `soldAccounts` int(11) NOT NULL DEFAULT 0,
  `registerDate` datetime NOT NULL DEFAULT current_timestamp(),
  `seller` tinyint(1) NOT NULL DEFAULT 0,
  `negativeRates` int(11) DEFAULT 0,
  `positiveRates` int(11) NOT NULL DEFAULT 0,
  `email` text NOT NULL,
  `confirmed` tinyint(1) NOT NULL DEFAULT 0,
  `lastConfirm` datetime NOT NULL DEFAULT current_timestamp(),
  `validateToken` text NOT NULL,
  `lastChangedPassword` datetime NOT NULL DEFAULT current_timestamp(),
  `lastIP` text NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Eliminarea datelor din tabel `users`
--

INSERT INTO `users` (`id`, `username`, `password`, `cookie`, `balance`, `admin`, `boughtAccounts`, `soldAccounts`, `registerDate`, `seller`, `negativeRates`, `positiveRates`, `email`, `confirmed`, `lastConfirm`, `validateToken`, `lastChangedPassword`, `lastIP`) VALUES
(1, 'denis', 'denissecured', '1701145277', '2342.60', 1, 3, 15, '2020-03-18 12:18:15', 1, 1, 0, 'asd', 1, '2020-05-17 17:23:02', 'QO81MY2ZSZOOTTH', '2020-05-18 01:05:03', '192.168.88.254'),
(2, 'raul2k', '420secured', '-929028032', '0.00', 1, 0, 0, '2020-05-14 17:55:25', 1, 0, 0, 'raulsa252@gmail.com', 1, '2020-05-18 04:14:39', 'LGHJMG5YEISH88XEPXYGA', '2020-05-17 20:38:26', '79.114.28.201'),
(5, 'ceauder', 'test', '', '0.00', 0, 0, 0, '2020-05-17 16:15:01', 0, 0, 0, 'picoloware@yahoo.com', 0, '2020-05-17 17:23:02', 'JJ6NYD3AWAP1TW', '2020-05-17 20:38:26', ''),
(9, 'asd', 'asd', '', '0.00', 0, 0, 0, '2020-05-17 17:19:02', 0, 0, 0, 'asd2as@as.ro', 0, '2020-05-18 13:59:19', 'INCGJCLWT4XTKEWE9FFYLMYVT1', '2020-05-17 20:38:26', ''),
(10, 'AlexConstangeles', 'Alexleu123', '', '0.00', 0, 0, 0, '2020-04-15 20:56:51', 0, 0, 0, 'jetssel942@gmail.com', 0, '2020-05-17 20:56:51', 'MSWAL08J9U7XTVQK0NXSRYHZV5DLO', '2020-05-17 20:56:51', ''),
(11, 'lightsquare', 'muie', '-1381954062', '0.00', 0, 0, 0, '2020-05-18 03:27:20', 0, 0, 0, 'teodscarlat32@gmail.com', 1, '2020-05-18 03:27:20', 'K2FL9DZ9NR9S6H7AGB7', '2020-05-18 03:27:20', '5.14.111.118'),
(13, 'mDenis16', 'mDenis16', '', '0.00', 0, 0, 0, '2020-05-18 13:38:09', 0, 0, 0, 'grasutu8@gmail.com', 0, '2020-05-18 14:29:55', 'MPSIRJ14NZGOAPM0GO', '2020-05-18 13:38:09', ''),
(14, 'lighty', 'muika', '-498014330', '207.00', 0, 0, 0, '2020-05-18 16:06:38', 1, 1, 0, 'teodscarlat@gmail.com', 1, '2020-05-18 16:06:39', 'FYMCSUC5ZWQ4UYTPAIC8L4E', '2020-05-18 16:11:02', '5.14.106.79'),
(15, 'clau', 'Clau123!', '989476211', '0.00', 1, 0, 0, '2020-05-18 18:36:02', 1, 0, 0, 'clau.burrito@gmail.com', 1, '2020-05-18 18:39:53', 'DO7RF839MM3MIVULEC8WO7DZG0', '2020-05-18 18:36:02', '192.168.88.254');

--
-- Indexuri pentru tabele eliminate
--

--
-- Indexuri pentru tabele `csgoaccounts`
--
ALTER TABLE `csgoaccounts`
  ADD PRIMARY KEY (`id`);

--
-- Indexuri pentru tabele `rates`
--
ALTER TABLE `rates`
  ADD PRIMARY KEY (`id`);

--
-- Indexuri pentru tabele `tickets`
--
ALTER TABLE `tickets`
  ADD PRIMARY KEY (`id`);

--
-- Indexuri pentru tabele `ticketsdata`
--
ALTER TABLE `ticketsdata`
  ADD PRIMARY KEY (`id`);

--
-- Indexuri pentru tabele `transactions`
--
ALTER TABLE `transactions`
  ADD PRIMARY KEY (`id`);

--
-- Indexuri pentru tabele `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`id`);

--
-- AUTO_INCREMENT pentru tabele eliminate
--

--
-- AUTO_INCREMENT pentru tabele `csgoaccounts`
--
ALTER TABLE `csgoaccounts`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

--
-- AUTO_INCREMENT pentru tabele `rates`
--
ALTER TABLE `rates`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT pentru tabele `tickets`
--
ALTER TABLE `tickets`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- AUTO_INCREMENT pentru tabele `ticketsdata`
--
ALTER TABLE `ticketsdata`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=60;

--
-- AUTO_INCREMENT pentru tabele `transactions`
--
ALTER TABLE `transactions`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=31;

--
-- AUTO_INCREMENT pentru tabele `users`
--
ALTER TABLE `users`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=16;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
