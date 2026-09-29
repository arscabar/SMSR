package smsr;

final class ValueBudget {
    int work,rows;
    void tick() { if(++work>250_000) throw new IllegalArgumentException("Value work limit"); }
    void charge() { tick(); if(++rows>20_000) throw new IllegalArgumentException("Value output limit"); }
}
