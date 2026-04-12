package com.finance.scheduler;

import com.finance.services.RecurringTransactionService;
import lombok.RequiredArgsConstructor;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

@Component
@RequiredArgsConstructor
public class RecurringTransactionScheduler {

    private static final Logger log = LoggerFactory.getLogger(RecurringTransactionScheduler.class);

    private final RecurringTransactionService recurringTransactionService;

    @Scheduled(cron = "0 0 * * * *")
    public void runRecurringTransactions() {
        log.info("Running recurring transaction scheduler");
        recurringTransactionService.processDueTransactions();
    }
}
