# created with help of github copilot

library(tidyverse)
library(patchwork)

raw <- read_csv("plot_data.csv")

glimpse(raw)

long <- raw |>
  pivot_longer(
    cols      = -participant,
    names_to  = c("measure", "condition"),
    names_sep = "_",
    values_to = "value"
  ) |>
  mutate(
    # Lock the display order: Visual on the left, Textual on the right
    condition = factor(condition, levels = c("visual", "textual"),
                       labels = c("Visual", "Textual")),
    # Lock the panel order: H1, H2, H3, H4
    measure = factor(measure,
                     levels = c("explain", "acc", "trust", "brier"),
                     labels = c("H1: Explainability",
                                "H2: Prediction Accuracy",
                                "H3: Performance Trust",
                                "H4: Brier Score"))
  )
long

condition_colours <- c("Visual" = "#D97706",   # amber
                       "Textual" = "#2563EB")  # blue

make_panel <- function(data, measure_label, y_label = NULL) {
  
  d <- data |> filter(measure == measure_label)
  
  summary_d <- d |>
    group_by(condition) |>
    summarise(
      mean_val = mean(value, na.rm = TRUE),
      se       = sd(value, na.rm = TRUE) / sqrt(sum(!is.na(value))),
      ci_low   = mean_val - 1.96 * se,
      ci_high  = mean_val + 1.96 * se,
      .groups  = "drop"
    )
  
  ggplot(d, aes(x = condition, y = value)) +
    # Thin grey participant lines
    geom_line(aes(group = participant),
              colour = "grey70", alpha = 0.5, linewidth = 0.4) +
    geom_point(colour = "grey60", alpha = 0.5, size = 1.2) +
    
    # Condition means with CI on top
    geom_errorbar(data = summary_d,
                  aes(x = condition, y = mean_val,
                      ymin = ci_low, ymax = ci_high,
                      colour = condition),
                  width = 0.12, linewidth = 0.8,
                  inherit.aes = FALSE) +
    geom_point(data = summary_d,
               aes(x = condition, y = mean_val, colour = condition),
               size = 4, inherit.aes = FALSE) +
    
    scale_colour_manual(values = condition_colours, guide = "none") +
    labs(title = measure_label,
         x = NULL,
         y = y_label %||% "Score") +
    theme_minimal(base_size = 11) +
    theme(
      plot.title      = element_text(face = "bold", size = 11),
      panel.grid.minor = element_blank(),
      panel.grid.major.x = element_blank()
    )
}

p1 <- make_panel(long, "H1: Explainability",
                 y_label = "Explainability (1–7)")
p1



p_h1 <- make_panel(long, "H1: Explainability",
                   y_label = "Explainability (1–7)")
p_h2 <- make_panel(long, "H2: Prediction Accuracy",
                   y_label = "Proportion correct (0–1)")
p_h3 <- make_panel(long, "H3: Performance Trust",
                   y_label = "Performance Trust (0–7)")
p_h4 <- make_panel(long, "H4: Brier Score",
                   y_label = "Brier score (lower = better)")


annotate_sig <- function(p, label) {
  p + annotate("text",
               x = 1.5, y = Inf, label = label,
               vjust = 1.5, size = 3.8, fontface = "bold",
               colour = "grey25")
}

p_h1 <- annotate_sig(p_h1, "*  p = .037")
p_h2 <- annotate_sig(p_h2, "***  p < .001")
p_h3 <- annotate_sig(p_h3, "ns  p = .087")
p_h4 <- annotate_sig(p_h4, "**  p = .009")


figure1 <- (p_h1 | p_h2) / (p_h3 | p_h4) +
  plot_annotation(
    title    = "Per-participant scores across modalities",
    subtitle = "Thin grey lines: individual participants (N = 36). Coloured points: condition means with 95% CI.",
    theme    = theme(
      plot.title    = element_text(face = "bold", size = 13),
      plot.subtitle = element_text(size = 10, colour = "grey30")
    )
  )

figure1

ggsave("figure1_paired_slopes.pdf", figure1,
       width = 9, height = 7, units = "in")
ggsave("figure1_paired_slopes.png", figure1,
       width = 9, height = 7, units = "in", dpi = 300)


