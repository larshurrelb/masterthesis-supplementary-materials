# created with help of github copilot

library(tidyverse)

df <- read_csv("plot_data3.csv")


per_step_cols <- df %>%
  select(participantId,
         matches("^(visual|textual)_step\\d+_(predictionCorrect|confidence)$"))

long <- per_step_cols %>%
  pivot_longer(
    cols      = -participantId,
    names_to  = c("condition", "step", "metric"),
    names_pattern = "^(visual|textual)_step(\\d+)_(predictionCorrect|confidence)$"
  ) %>%
  pivot_wider(names_from = metric, values_from = value) %>%
  filter(!is.na(predictionCorrect), !is.na(confidence)) %>%
  mutate(
    condition  = factor(condition,
                        levels = c("visual", "textual"),
                        labels = c("Visual", "Textual")),
    confidence = as.integer(confidence),
    predictionCorrect = as.integer(predictionCorrect),
    stated_prob = (confidence - 1) / 4
  )

wilson_ci <- function(k, n, conf = 0.95) {
  if (n == 0) return(tibble(lo = NA_real_, hi = NA_real_))
  z  <- qnorm(1 - (1 - conf) / 2)
  p  <- k / n
  denom  <- 1 + z^2 / n
  centre <- (p + z^2 / (2 * n)) / denom
  halfw  <- (z * sqrt(p * (1 - p) / n + z^2 / (4 * n^2))) / denom
  tibble(lo = centre - halfw, hi = centre + halfw)
}

bin_summary <- long %>%
  group_by(condition, confidence, stated_prob) %>%
  summarise(
    n_trials     = n(),
    n_correct    = sum(predictionCorrect),
    prop_correct = mean(predictionCorrect),
    .groups = "drop"
  ) %>%
  rowwise() %>%
  mutate(ci = list(wilson_ci(n_correct, n_trials))) %>%
  unnest(ci) %>%
  ungroup()

print(bin_summary)

cond_colours <- c(Visual = "#D97706", Textual = "#2563EB")

p_main <- ggplot(bin_summary,
                 aes(x = stated_prob, y = prop_correct,
                     colour = condition, fill = condition)) +
  geom_abline(slope = 1, intercept = 0,
              linetype = "dashed", colour = "grey40") +
  geom_errorbar(aes(ymin = lo, ymax = hi),
                width = 0.015, alpha = 0.7, linewidth = 0.5) +
  geom_line(linewidth = 0.9) +
  geom_point(aes(size = n_trials),
             shape = 21, colour = "white", stroke = 0.8) +
  scale_x_continuous(
    "Stated confidence",
    breaks = c(0, 0.25, 0.5, 0.75, 1),
    labels = c("1\n not at all", "2", "3", "4", "5\n very"),
    limits = c(-0.05, 1.05)
  ) +
  scale_y_continuous(
    "Proportion of predictions correct",
    breaks = seq(0, 1, 0.25),
    limits = c(-0.05, 1.05)
  ) +
  scale_colour_manual(values = cond_colours, name = NULL) +
  scale_fill_manual(values = cond_colours, name = NULL) +
  scale_size_area(max_size = 8, guide = "none") +
  coord_equal() +
  annotate("text", x = 0.93, y = 0.98,
           label = "Perfect calibration",
           angle = 45, vjust = -0.4, size = 3, colour = "grey40") +
  theme_minimal(base_size = 11) +
  theme(
    panel.grid.minor = element_blank(),
    legend.position  = c(0.18, 0.90),
    legend.background = element_rect(fill = "white", colour = NA),
    plot.title = element_text(face = "bold")
  ) +
  labs(
    title = "Confidence calibration by modality",
    subtitle =
      "Points on the diagonal indicate well-calibrated confidence; points above the\ndiagonal indicate under-confidence, points below indicate over-confidence. Point\nsize is proportional to the number of trials in the confidence bin."
  )

p_counts <- ggplot(bin_summary,
                   aes(y = factor(confidence, levels = 5:1),
                       x = n_trials, fill = condition)) +
  geom_col(position = position_dodge(width = 0.7), width = 0.65) +
  scale_x_continuous("Number of trials",
                     expand = expansion(mult = c(0, 0.05))) +
  scale_y_discrete("Stated confidence",
                   labels = c("5" = "5 very",
                              "4" = "4",
                              "3" = "3",
                              "2" = "2",
                              "1" = "1 not at all")) +
  scale_fill_manual(values = cond_colours, guide = "none") +
  ggtitle("Trials per confidence bin") +
  theme_minimal(base_size = 10) +
  theme(panel.grid.minor = element_blank(),
        plot.title = element_text(face = "bold", size = 10),
        plot.margin = margin(5, 5, 5, 10))

if (requireNamespace("patchwork", quietly = TRUE)) {
  library(patchwork)
  final <- p_main + p_counts + plot_layout(widths = c(5, 1.2))
} else {
  final <- p_main
}

ggsave("figure3_reliability_diagram.png",
       final, width = 11, height = 7, dpi = 300, bg = "white")

cat("Saved: figure3_reliability_diagram.png\n")