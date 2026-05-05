# created with help of github copilot

library(tidyverse)

raw <- read_csv("plot_data2.csv")

glimpse(raw)

lifts <- raw |>
  transmute(
    participant,
    Visual  = acc_visual  - acc_baseline,
    Textual = acc_textual - acc_baseline
  ) |>
  pivot_longer(
    cols      = c(Visual, Textual),
    names_to  = "condition",
    values_to = "lift"
  ) |>
  mutate(condition = factor(condition, levels = c("Visual", "Textual")))

lifts   

condition_colours <- c("Visual"  = "#D97706",
                       "Textual" = "#2563EB")


summary_d <- lifts |>
  group_by(condition) |>
  summarise(
    mean_val = mean(lift, na.rm = TRUE),
    se       = sd(lift, na.rm = TRUE) / sqrt(sum(!is.na(lift))),
    ci_low   = mean_val - 1.96 * se,
    ci_high  = mean_val + 1.96 * se,
    .groups  = "drop"
  )

summary_d


figure2 <- ggplot(lifts, aes(x = condition, y = lift)) +
  
  
  geom_hline(yintercept = 0, colour = "grey40",
             linetype = "dashed", linewidth = 0.5) +
  annotate("text", x = -Inf, y = 0.02,
           label = "baseline (no modality)",
           hjust = -0.05, vjust = 0,
           size = 3, colour = "grey45", fontface = "italic") +
  
  
  geom_line(aes(group = participant),
            colour = "grey70", alpha = 0.5, linewidth = 0.4) +
  geom_point(colour = "grey60", alpha = 0.5, size = 1.3) +
  
  
  geom_errorbar(data = summary_d,
                aes(x = condition, y = mean_val,
                    ymin = ci_low, ymax = ci_high,
                    colour = condition),
                width = 0.1, linewidth = 0.9, inherit.aes = FALSE) +
  geom_point(data = summary_d,
             aes(x = condition, y = mean_val, colour = condition),
             size = 4.5, inherit.aes = FALSE) +
  
  # Mean labels next to the condition dots
  geom_text(data = summary_d,
            aes(x = condition, y = mean_val,
                label = sprintf("M = %+.2f", mean_val)),
            hjust = -0.35, vjust = 0.5,
            size = 3.6, fontface = "bold",
            inherit.aes = FALSE) +
  
  scale_colour_manual(values = condition_colours, guide = "none") +
  scale_y_continuous(
    labels = scales::label_number(accuracy = 0.1, style_positive = "plus"),
    breaks = seq(-0.4, 1.0, by = 0.2)
  ) +
  labs(
    title    = "Both modalities lift prediction accuracy above baseline, but textual lifts more",
    subtitle = sprintf(
      "Baseline (no-bubble) accuracy: M = %.2f, SD = %.2f. N = 36.",
      mean(raw$acc_baseline), sd(raw$acc_baseline)
    ),
    x = NULL,
    y = "Lift over baseline (Δ proportion correct)"
  ) +
  theme_minimal(base_size = 11) +
  theme(
    plot.title         = element_text(face = "bold", size = 12),
    plot.subtitle      = element_text(size = 10, colour = "grey30"),
    panel.grid.minor   = element_blank(),
    panel.grid.major.x = element_blank()
  )

figure2


ggsave("figure2_baseline_lift.pdf", figure2,
       width = 7, height = 5.5, units = "in")
ggsave("figure2_baseline_lift.png", figure2,
       width = 7, height = 5.5, units = "in", dpi = 300)
